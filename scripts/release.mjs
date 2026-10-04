#!/usr/bin/env node
// A release is a manifest version with no `<id>@<version>` tag yet; --check fails a package changed since its tag without a bump.
import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const root = new URL('..', import.meta.url).pathname;
const args = new Set(process.argv.slice(2));
const mode = args.has('--check') ? 'check' : args.has('--dry-run') ? 'dry-run' : 'release';

const run = (cmd, argv, opts = {}) =>
  (execFileSync(cmd, argv, { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'inherit'], ...opts }) ?? '').trim();

const npmPublished = (p) => {
  try {
    return run('npm', ['view', `${p.id}@${p.version}`, 'version', '--registry', 'https://npm.pkg.github.com'], { stdio: ['ignore', 'pipe', 'ignore'] }) === p.version;
  } catch {
    return false;
  }
};

function npmPackages() {
  const dir = join(root, 'packages');
  if (!existsSync(dir)) return [];
  return readdirSync(dir)
    .filter((name) => existsSync(join(dir, name, 'package.json')))
    .map((name) => {
      const manifest = JSON.parse(readFileSync(join(dir, name, 'package.json'), 'utf8'));
      const deps = { ...manifest.dependencies, ...manifest.peerDependencies };
      return { kind: 'npm', id: manifest.name, version: manifest.version, dir: `packages/${name}`, private: manifest.private === true, deps: Object.keys(deps) };
    })
    .filter((p) => !p.private);
}

function nugetPackages() {
  const dir = join(root, 'src');
  if (!existsSync(dir)) return [];
  return readdirSync(dir)
    .filter((name) => existsSync(join(dir, name, `${name}.csproj`)))
    .map((name) => {
      const csproj = readFileSync(join(dir, name, `${name}.csproj`), 'utf8');
      const version = csproj.match(/<Version>([^<]+)<\/Version>/)?.[1];
      if (!version) throw new Error(`src/${name}/${name}.csproj has no <Version>`);
      const deps = [...csproj.matchAll(/<ProjectReference\s+Include="[^"]*?([^"\\/]+)\.csproj"/g)].map((m) => m[1]);
      return { kind: 'nuget', id: name, version, dir: `src/${name}`, deps };
    });
}

function inDependencyOrder(packages) {
  const byId = new Map(packages.map((p) => [p.id, p]));
  const ordered = [];
  const seen = new Set();
  const visit = (p, trail) => {
    if (seen.has(p.id)) return;
    if (trail.includes(p.id)) throw new Error(`dependency cycle: ${[...trail, p.id].join(' -> ')}`);
    for (const dep of p.deps) if (byId.has(dep)) visit(byId.get(dep), [...trail, p.id]);
    seen.add(p.id);
    ordered.push(p);
  };
  for (const p of packages) visit(p, []);
  return ordered;
}

const semverKey = (v) => v.split(/[.-]/).map((part) => (/^\d+$/.test(part) ? part.padStart(8, '0') : part)).join('.');

function lastTag(id) {
  const tags = run('git', ['tag', '--list', `${id}@*`]).split('\n').filter(Boolean);
  return tags.sort((a, b) => semverKey(a.slice(id.length + 1)).localeCompare(semverKey(b.slice(id.length + 1)))).at(-1);
}

const changelogSection = (p) => {
  const file = join(root, p.dir, 'CHANGELOG.md');
  if (!existsSync(file)) return undefined;
  const text = readFileSync(file, 'utf8');
  const start = text.indexOf(`\n## ${p.version}\n`);
  if (start < 0) return undefined;
  const body = text.slice(start + `\n## ${p.version}\n`.length);
  const next = body.search(/\n## /);
  return (next < 0 ? body : body.slice(0, next)).trim();
};

const packages = inDependencyOrder([...npmPackages(), ...nugetPackages()]);

if (mode === 'check') {
  const failures = [];
  for (const p of packages) {
    const tag = lastTag(p.id);
    const tagged = tag?.slice(p.id.length + 1);
    if (tagged === p.version) {
      const changed = run('git', ['diff', '--name-only', tag, '--', p.dir]);
      if (changed) failures.push(`${p.id}: files changed since ${tag} but version is still ${p.version}`);
      continue;
    }
    if (!changelogSection(p)) failures.push(`${p.id}: CHANGELOG.md has no "## ${p.version}" section`);
  }
  for (const f of failures) console.error(f);
  if (failures.length) process.exit(1);
  console.log(`release check passed for ${packages.length} packages`);
  process.exit(0);
}

const unreleased = packages.filter((p) => !run('git', ['tag', '--list', `${p.id}@${p.version}`]));
if (!unreleased.length) {
  console.log('nothing to release');
  process.exit(0);
}
for (const p of unreleased) console.log(`${p.kind}\t${p.id}@${p.version}`);
if (mode === 'dry-run') process.exit(0);

const token = process.env.GITHUB_TOKEN;
if (!token) throw new Error('GITHUB_TOKEN is required to publish');
const out = mkdtempSync(join(tmpdir(), 'lupira-release-'));

for (const p of unreleased) {
  const notes = changelogSection(p);
  if (!notes) throw new Error(`${p.id}: CHANGELOG.md has no "## ${p.version}" section`);
  if (p.kind === 'npm') {
    // A run that died after publishing but before tagging leaves the version on the registry.
    if (!npmPublished(p)) run('npm', ['publish', '-w', p.dir], { stdio: 'inherit' });
  } else {
    run('dotnet', ['pack', p.dir, '-c', 'Release', '-o', out], { stdio: 'inherit' });
    run('dotnet', ['nuget', 'push', join(out, `${p.id}.${p.version}.nupkg`), '--source', 'https://nuget.pkg.github.com/danbro96/index.json', '--api-key', token, '--skip-duplicate'], { stdio: 'inherit' });
  }
  const tag = `${p.id}@${p.version}`;
  run('git', ['tag', tag]);
  run('git', ['push', 'origin', tag], { stdio: 'inherit' });
  run('gh', ['release', 'create', tag, '--title', tag, '--notes', notes], { stdio: 'inherit' });
}
