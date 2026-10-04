#!/usr/bin/env node
// Vendors the maplibre-gl worker + its shared-module import verbatim into the app's public/maplibre/ (see maplibreSetup).
import { copyFileSync, mkdirSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join, resolve } from 'node:path';

const out = resolve(process.argv[2] ?? 'public/maplibre');
const dist = join(dirname(createRequire(join(process.cwd(), 'package.json')).resolve('maplibre-gl/package.json')), 'dist');
mkdirSync(out, { recursive: true });
for (const file of ['maplibre-gl-worker.mjs', 'maplibre-gl-shared.mjs']) copyFileSync(join(dist, file), join(out, file));
