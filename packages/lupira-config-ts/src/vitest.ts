import { defineConfig, type ViteUserConfig } from 'vitest/config';

type TestOptions = NonNullable<ViteUserConfig['test']>;

/** Colocated node-environment unit tests; `test` overrides any of the defaults. */
export function vitestConfig(test: TestOptions = {}): ViteUserConfig {
  return defineConfig({
    test: {
      environment: 'node',
      include: ['src/**/*.test.ts'],
      ...test,
    },
  });
}
