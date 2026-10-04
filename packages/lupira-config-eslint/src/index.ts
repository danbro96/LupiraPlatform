import type { Linter } from 'eslint';
import boundaries from 'eslint-plugin-boundaries';
import reactHooks from 'eslint-plugin-react-hooks';
import tseslint from 'typescript-eslint';

type Policy = { from: { element: { type: string } }; allow: object[] };

/** v7 entity-selector helper: `to('domain','data')` → [{ to: { element: { type: 'domain' } } }, …]. */
const to = (...types: string[]) => types.map((t) => ({ to: { element: { type: t } } }));

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

/**
 * The online-only web client: downward-only data → state → ui, with `config` a leaf any layer may
 * import. Shared packages arrive as external imports and are allowed everywhere.
 */
export function web({ ignores = [] }: AppOptions = {}): Linter.Config[] {
  return [
    { ignores: ['node_modules/**', 'dist/**', ...ignores, ...CONFIG_FILES] },
    {
      files: ['src/**/*.{ts,tsx}'],
      languageOptions: parser(true),
      plugins: plugins(true),
      settings: {
        'boundaries/elements': [
          { type: 'data', pattern: 'src/data/**' },
          { type: 'state', pattern: 'src/state/**' },
          { type: 'ui', pattern: 'src/ui/**' },
          { type: 'config', pattern: 'src/config' },
        ],
        'import/resolver': RESOLVER,
      },
      rules: {
        'boundaries/dependencies': ['error', {
          default: 'disallow',
          policies: [
            { from: { element: { type: 'data' } }, allow: to('data', 'config') },
            { from: { element: { type: 'state' } }, allow: to('state', 'data', 'config') },
            { from: { element: { type: 'ui' } }, allow: to('ui', 'state', 'data', 'config') },
            { from: { element: { type: 'config' } }, allow: [] },
          ],
        }],
        ...hookRules(true),
      },
    },
  ];
}

export interface MobileOptions extends AppOptions {
  /** The orval output folder; its own element, importable from data upward. */
  generated?: string;
  /** Lets `domain` import the generated DTO types. */
  domainImportsGenerated?: boolean;
}

/**
 * The offline-first Expo app: downward-only domain → data → sync → state → ui. The cross-cutting
 * leaves (feedback, debug, config) may be imported by any layer above domain but import no app layer.
 */
export function mobile({ ignores = [], generated = 'src/data/api/generated', domainImportsGenerated = false }: MobileOptions = {}): Linter.Config[] {
  const leaves = ['feedback', 'debug', 'config'];
  return [
    { ignores: ['node_modules/**', '.expo/**', 'android/**', 'dist/**', `${generated}/**`, ...ignores, ...CONFIG_FILES] },
    {
      files: ['src/**/*.{ts,tsx}', 'App.tsx', 'index.ts'],
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
          { type: 'feedback', pattern: 'src/feedback/**' },
          { type: 'debug', pattern: 'src/debug/**' },
          { type: 'polyfills', pattern: 'src/polyfills/**' },
          { type: 'config', pattern: 'src/config' },
        ],
        'import/resolver': RESOLVER,
      },
      rules: {
        'boundaries/dependencies': ['error', {
          default: 'disallow',
          policies: [
            { from: { element: { type: 'generated' } }, allow: to('generated', 'data') },
            { from: { element: { type: 'domain' } }, allow: to('domain', ...(domainImportsGenerated ? ['generated'] : [])) },
            { from: { element: { type: 'data' } }, allow: to('data', 'domain', 'generated', ...leaves) },
            { from: { element: { type: 'sync' } }, allow: to('sync', 'data', 'domain', 'generated', ...leaves) },
            { from: { element: { type: 'state' } }, allow: to('state', 'sync', 'data', 'domain', 'generated', ...leaves) },
            { from: { element: { type: 'ui' } }, allow: to('ui', 'state', 'sync', 'data', 'domain', 'generated', ...leaves) },
            { from: { element: { type: 'feedback' } }, allow: to('feedback') },
            { from: { element: { type: 'debug' } }, allow: to('debug') },
            { from: { element: { type: 'polyfills' } }, allow: to('polyfills') },
            { from: { element: { type: 'config' } }, allow: [] },
          ],
        }],
        ...hookRules(true),
      },
    },
  ];
}
