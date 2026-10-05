import type { Linter } from 'eslint';
import boundaries from 'eslint-plugin-boundaries';
import reactHooks from 'eslint-plugin-react-hooks';
import tseslint from 'typescript-eslint';

type Policy = { from?: { element: { type: string } }; allow?: object[]; disallow?: object[] };

/** v7 entity-selector helper: `to('domain','data')` → [{ to: { element: { type: 'domain' } } }, …]. */
const to = (...types: string[]) => types.map((t) => ({ to: { element: { type: t } } }));

const platform = (source: string | string[], internalPath?: string | string[]) => ({
  to: { module: { origin: 'external', source, ...(internalPath && { internalPath }) } },
});

const fromEach = (types: string[], allow: object[]): Policy[] => types.map((t) => ({ from: { element: { type: t } }, allow }));

const EXTERNAL_MODULES: Policy = { allow: [{ to: { module: { origin: ['external', 'core'] } } }] } as unknown as Policy;
const NO_PLATFORM_BY_DEFAULT: Policy = { disallow: [platform('@danbro96/*')] } as unknown as Policy;
const EVERYWHERE: Policy = { allow: [platform(['@danbro96/lupira-tokens-*', '@danbro96/lupira-domain-*', '@danbro96/lupira-sync-core'])] } as unknown as Policy;

const CONFIG_FILES = ['*.config.js', '*.config.mjs', '*.config.ts', '*.config.mts'];

const RESOLVER = { typescript: { alwaysTryTypes: true } };

const parser = (jsx: boolean): Linter.Config['languageOptions'] => ({
  parser: tseslint.parser as Linter.Parser,
  ...(jsx ? { parserOptions: { ecmaFeatures: { jsx: true } } } : {}),
});

const plugins = (react: boolean): Linter.Config['plugins'] => ({
  boundaries: boundaries as unknown as NonNullable<Linter.Config['plugins']>[string],
  ...(react ? { 'react-hooks': reactHooks as unknown as NonNullable<Linter.Config['plugins']>[string] } : {}),
});

const hookRules = (react: boolean): Linter.RulesRecord => (react ? reactHooks.configs['recommended-latest'].rules : {});

export interface PureOptions {
  /** The element name lint messages report the package as. */
  element?: string;
  /** npm modules production code may import, e.g. the package's peer dependencies or a types-only package. */
  allowModules?: string[];
  /** JSX parsing plus the hook and React Compiler rules. */
  react?: boolean;
}

/**
 * Purity by construction: production modules may import nothing but each other and `allowModules`.
 * Test files get a trailing override block (v7 element patterns match folders, so a test-file glob
 * element could never classify them).
 */
export function pure({ element = 'package', allowModules = [], react = false }: PureOptions = {}): Linter.Config[] {
  const policies: Policy[] = [{ from: { element: { type: element } }, allow: to(element) }];
  if (allowModules.length) {
    policies.push({
      from: { element: { type: element } },
      allow: [{ to: { module: { origin: ['external', 'core'], source: allowModules } } }],
    });
  }
  return [
    { ignores: ['node_modules/**', 'dist/**', ...CONFIG_FILES] },
    {
      files: ['src/**/*.{ts,tsx}'],
      languageOptions: parser(react),
      plugins: plugins(react),
      settings: {
        'boundaries/elements': [{ type: element, pattern: 'src/**' }],
        'import/resolver': RESOLVER,
      },
      rules: {
        // checkAllOrigins widens the rule from local elements to npm imports as well.
        'boundaries/dependencies': ['error', { checkAllOrigins: true, default: 'disallow', policies }],
        ...hookRules(react),
      },
    },
    {
      // Tests may use the runner + node builtins for fixtures; the boundary gate is for production code.
      files: ['src/**/*.test.{ts,tsx}'],
      rules: {
        'boundaries/dependencies': ['error', { default: 'disallow', policies }],
      },
    },
  ];
}

export interface AppOptions {
  /** Generated clients and other non-authored paths, on top of build output and config files. */
  ignores?: string[];
}

export interface WebOptions extends AppOptions {
  /** Adds the pure `domain` layer below data. */
  domain?: boolean;
}

/**
 * The online-only web client: downward-only (domain →) data → state → ui, with `config` a leaf any layer may
 * import. Platform packages sit at the layer their name declares.
 */
export function web({ ignores = [], domain = false }: WebOptions = {}): Linter.Config[] {
  const below = domain ? ['domain'] : [];
  const policies: Policy[] = [
    ...(domain ? [{ from: { element: { type: 'domain' } }, allow: to('domain') }] : []),
    { from: { element: { type: 'data' } }, allow: to('data', ...below, 'config') },
    { from: { element: { type: 'state' } }, allow: to('state', 'data', ...below, 'config') },
    { from: { element: { type: 'ui' } }, allow: to('ui', 'state', 'data', ...below, 'config') },
    { from: { element: { type: 'config' } }, allow: [] },
    EXTERNAL_MODULES,
    NO_PLATFORM_BY_DEFAULT,
    EVERYWHERE,
    ...fromEach(['data', 'state', 'ui'], [platform('@danbro96/lupira-http')]),
    ...fromEach(['data', 'state', 'ui'], [platform('@danbro96/lupira-web-session', ['session', 'cookieTransport'])]),
    ...fromEach(['state', 'ui'], [platform('@danbro96/lupira-web-session', 'useSession')]),
    { from: { element: { type: 'ui' } }, allow: [platform('@danbro96/lupira-web-*')] },
  ];
  return [
    { ignores: ['node_modules/**', 'dist/**', ...ignores, ...CONFIG_FILES] },
    {
      files: ['src/**/*.{ts,tsx}'],
      languageOptions: parser(true),
      plugins: plugins(true),
      settings: {
        'boundaries/elements': [
          ...(domain ? [{ type: 'domain', pattern: 'src/domain/**' }] : []),
          { type: 'data', pattern: 'src/data/**' },
          { type: 'state', pattern: 'src/state/**' },
          { type: 'ui', pattern: 'src/ui/**' },
          { type: 'config', pattern: 'src/config/**' },
        ],
        'import/resolver': RESOLVER,
      },
      rules: {
        'boundaries/dependencies': ['error', { default: 'disallow', policies, checkAllOrigins: true }],
        ...hookRules(true),
      },
    },
  ];
}

/** A folder of the mobile app outside the standard stack, e.g. a headless background-task layer. */
export interface ExtraLayer {
  name: string;
  pattern: string;
  /** Standard layers (and other extra layers) this one may import. */
  imports: string[];
  /** Standard layers that may import this one. */
  importedBy: string[];
}

export interface MobileOptions extends AppOptions {
  /** The orval output folder; its own element, importable from data upward. */
  generated?: string;
  /** Lets `domain` import the generated DTO types. */
  domainImportsGenerated?: boolean;
  layers?: ExtraLayer[];
}

const STACK = ['data', 'sync', 'state', 'ui'];

/**
 * The offline-first Expo app: downward-only domain → data → sync → state → ui, with `config` a leaf. Platform
 * packages sit at the layer their name declares; `expo-sqlite/node` is for tests only.
 */
export function mobile({ ignores = [], generated = 'src/data/api/generated', domainImportsGenerated = false, layers = [] }: MobileOptions = {}): Linter.Config[] {
  const extra = layers.map((l) => l.name);
  const importedBy = (layer: string) => layers.filter((l) => l.importedBy.includes(layer)).map((l) => l.name);
  const stack = (layer: string, imports: string[]) =>
    ({ from: { element: { type: layer } }, allow: to(layer, ...imports, ...importedBy(layer), 'config') }) as Policy;
  const dataUp = [
    platform('@danbro96/lupira-http'),
    platform('@danbro96/lupira-expo-feedback'),
    platform('@danbro96/lupira-expo-diagnostics', 'log'),
    platform('@danbro96/lupira-expo-oidc', ['oidc', 'tokenSession']),
    platform('@danbro96/lupira-expo-sqlite', ['types', 'expoDb', 'migrate']),
  ];
  const policies = (...more: Policy[]): Policy[] => [
    { from: { element: { type: 'generated' } }, allow: to('generated', 'data') },
    { from: { element: { type: 'domain' } }, allow: to('domain', ...(domainImportsGenerated ? ['generated'] : [])) },
    stack('data', ['domain', 'generated']),
    stack('sync', ['data', 'domain', 'generated']),
    stack('state', ['sync', 'data', 'domain', 'generated']),
    stack('ui', ['state', 'sync', 'data', 'domain', 'generated']),
    ...layers.map((l) => ({ from: { element: { type: l.name } }, allow: to(l.name, ...l.imports, 'config') }) as Policy),
    { from: { element: { type: 'config' } }, allow: [] },
    EXTERNAL_MODULES,
    NO_PLATFORM_BY_DEFAULT,
    EVERYWHERE,
    { from: { element: { type: 'domain' } }, allow: [platform('@danbro96/lupira-http', 'apiError')] },
    ...fromEach([...STACK, ...extra], dataUp),
    ...fromEach(['sync', 'state', 'ui'], [platform(['@danbro96/lupira-sync-engine', '@danbro96/lupira-expo-query'])]),
    { from: { element: { type: 'ui' } }, allow: [platform(['@danbro96/lupira-expo-paper', '@danbro96/lupira-expo-diagnostics'])] },
    ...more,
  ];
  const files = ['src/**/*.{ts,tsx}', 'App.tsx', 'index.ts'];
  return [
    { ignores: ['node_modules/**', '.expo/**', 'android/**', 'dist/**', `${generated}/**`, ...ignores, ...CONFIG_FILES] },
    {
      files,
      languageOptions: parser(true),
      plugins: plugins(true),
      settings: {
        'boundaries/elements': [
          { type: 'generated', pattern: `${generated}/**` },
          { type: 'domain', pattern: 'src/domain/**' },
          { type: 'data', pattern: 'src/data/**' },
          { type: 'sync', pattern: 'src/sync/**' },
          { type: 'state', pattern: 'src/state/**' },
          { type: 'ui', pattern: 'src/ui/**' },
          ...layers.map((l) => ({ type: l.name, pattern: l.pattern })),
          { type: 'config', pattern: 'src/config/**' },
        ],
        'import/resolver': RESOLVER,
      },
      rules: {
        'boundaries/dependencies': ['error', { default: 'disallow', policies: policies(), checkAllOrigins: true }],
        ...hookRules(true),
        'no-restricted-imports': ['error', { paths: [{ name: '@danbro96/lupira-expo-sqlite/node', message: 'node:sqlite is for tests; Metro cannot bundle it.' }] }],
      },
    },
    {
      files: ['src/**/*.test.{ts,tsx}'],
      rules: {
        'boundaries/dependencies': ['error', {
          default: 'disallow',
          policies: policies({ allow: [platform('@danbro96/lupira-expo-sqlite', 'node')] } as unknown as Policy),
          checkAllOrigins: true,
        }],
        'no-restricted-imports': 'off',
      },
    },
  ];
}
