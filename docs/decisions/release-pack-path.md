# Decision — One release path per package: `dotnet pack -c Release` from an emptied `bin/`

**Status:** accepted

**Context:** Released packages carried files they should not. ExitPointGaps 1.1.1 on nuget.org shipped seven files of the pre-rename `CoverageGap.Tool` ([rework record](exitpointgaps-rework.md)), because publish never deletes what an earlier build left in its folder. Two paths also wrote packages: a Release build packed the libraries and the analyzer into `artifacts/` (`GeneratePackageOnBuild`), and the release guide packed into `artifacts/release/`. Every Release build replaced the local package of the current version, and a local restore of that version could get a copy from `~/.nuget/packages` that came from an earlier build. The maintainer asked for one explicit path that cleans, builds in Release, and leaves clean artifacts in `artifacts/`.

**Decision:** `dotnet pack src/{Project}/{Project}.csproj -c Release` is the only way to a package, for libraries, the analyzer, and dotnet tools. It writes `artifacts/release/{PackageId}.{version}.nupkg`. Before such a build compiles, each project of it, project references included, empties its own `$(OutDir)`; the build detects the pack through the global property `_IsPacking`, which `dotnet pack` sets and the SDK itself reads. A tool also empties `$(PublishDir)` before every publish. Every other way into a package stops with an error that names the command: a Debug pack, `--no-build`, and a pack without `_IsPacking` (an IDE, `dotnet msbuild -t:Pack`). The pack deletes this version's old package first. A build never packs. The sample's packed-package check restores from `artifacts/release/` into a private packages folder.

**Rejected:**
- Keep packing on Release builds: each build overwrites the package of the current version, and a tool packs before its publish files exist (MSB3030).
- `dotnet clean` before the pack: it deletes only the files it listed itself; the stale assembly of a renamed project survives.
- A script that deletes `bin/` and then packs: Section 4.7 forbids repository scripts, and the clean must also reach project references.
- An isolated output tree per release (`--artifacts-path`): it moves `bin/` and `obj/` of every project, and the analyzer wiring of this repository reads `ExitPoints.dll` from the checker's `bin/`, so it would read a stale file.
- An explicit opt-in property (`-p:Release=true`): a pack without it would still be possible and unclean; `_IsPacking` makes every CLI pack the clean one.
- Delete `obj/` too: restore has already written `project.assets.json` there before the build starts; the build regenerates what it packs from `obj/`.

**Consequences:** A release needs no manual delete step and cannot pick up old binaries from `bin/`. A Release build leaves `artifacts/release/` alone. Packing from an IDE stops with an error, by intent. `dotnet pack` deletes the `bin/Release` output of the projects it builds, so a test run of another agent on the same tree can collide (Section 4.14). `_IsPacking` is an SDK-internal name: an SDK update that drops it turns off the clean and makes `RequireReleasePackPath` stop every pack with an error, which shows at once. The section in `tech-solution.md` brings that skill above the 12 KB budget of `tech-instructions.md`; it loads only when build files are in scope, and the snippet saves every other project from rebuilding the mechanism.

**Enforced in:** `CleanOutputBeforePack`, `RequireReleasePackPath`, and `CleanToolPublishDir` in [Directory.Build.targets](../../Directory.Build.targets); [`tech-solution.md`](../../.github/skills/tech-solution.md) § Release packages; [release guide](../guides/release-a-package.md) step 6.
