# LupiraPlatform — agent notes

- Shared npm + NuGet packages for the Lupira estate. Conventions, naming, release and consuming: `~/Nextcloud/Familj/DevOps/Guides/platform-packages.md`.
- **Layout**: `src/Lupira.<Area>.<Concern>/` (NuGet, in `LupiraPlatform.slnx`), `tests/<Package>.UnitTests/`, `packages/lupira-<layer>-<concern>/` (npm workspaces). `Directory.Build.props` imports `src/Lupira.Build/build/Lupira.Build.props` (dogfood).
- **Every change to a package** bumps its manifest version and adds `## <version>` to its `CHANGELOG.md` in the same commit; `node scripts/release.mjs --check` enforces it. Tags `<id>@<version>` are created by `release.yml`, never by hand.
- **Purity**: `Lupira.Primitives`, `Lupira.Results`, `Lupira.Contracts.*` stay BCL-only (`Lupira.Build.Tests`); `tokens`/`domain`/`sync` npm layers stay dependency-free (`lupira-config-eslint` `pure` preset).
- **Admission**: a package needs ≥2 consuming repos. Generated API clients, app shells and per-domain feed queries are never packaged.
- **React packages** (peer `react`) ship precompiled by React Compiler (`scripts/react-compile.mjs`, `panicThreshold: all_errors`); their tsconfig is `emitDeclarationOnly`. Apps' compiler skips `node_modules`.
- Build/test: `npm run clean` (stale `node_modules/.tmp` buildinfo skips emit), `npm run build && npm run typecheck && npm run lint && npm test`; `timeout 180 env DOTNET_GCHeapHardLimit=0x20000000 dotnet test LupiraPlatform.slnx -m:1`.
