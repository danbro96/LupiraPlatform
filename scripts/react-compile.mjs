#!/usr/bin/env node
// Apps run React Compiler but skip node_modules, so React packages ship precompiled; tsc emits only their declarations.
import { transformFileAsync } from '@babel/core';
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';

const root = new URL('..', import.meta.url).pathname;
const packagesDir = join(root, 'packages');

const isReactPackage = (dir) => {
  const manifest = JSON.parse(readFileSync(join(dir, 'package.json'), 'utf8'));
  return Boolean(manifest.peerDependencies?.react);
};

const sources = (dir) =>
  readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) return sources(path);
    return /\.tsx?$/.test(entry.name) && !/\.test\.tsx?$/.test(entry.name) && !entry.name.endsWith('.d.ts') ? [path] : [];
  });

let failed = false;
for (const name of readdirSync(packagesDir)) {
  const pkg = join(packagesDir, name);
  if (!existsSync(join(pkg, 'package.json')) || !isReactPackage(pkg)) continue;
  const src = join(pkg, 'src');
  for (const file of sources(src)) {
    const out = join(pkg, 'dist', relative(src, file)).replace(/\.tsx?$/, '.js');
    try {
      const result = await transformFileAsync(file, {
        babelrc: false,
        configFile: false,
        sourceMaps: true,
        sourceFileName: relative(dirname(out), file),
        presets: [['@babel/preset-typescript', { rewriteImportExtensions: true, onlyRemoveTypeImports: true }]],
        plugins: [
          ['babel-plugin-react-compiler', { target: '19', panicThreshold: 'all_errors' }],
          ['@babel/plugin-transform-react-jsx', { runtime: 'automatic' }],
        ],
      });
      mkdirSync(dirname(out), { recursive: true });
      writeFileSync(out, `${result.code}\n//# sourceMappingURL=${out.split('/').at(-1)}.map\n`);
      writeFileSync(`${out}.map`, JSON.stringify(result.map));
    } catch (error) {
      failed = true;
      console.error(`${relative(root, file)}: ${error.message}`);
    }
  }
}
if (failed) process.exit(1);
