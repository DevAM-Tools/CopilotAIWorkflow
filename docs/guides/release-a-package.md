# Guide — Release one package

**For:** maintainers · **Prerequisites:** .NET 10 SDK; a NuGet API key with push rights for the package, in an environment variable (never on the command line history or in a file of this repository).

Each package has its own version line and releases alone: CSharpStyleChecker, InvariantStringHelper, ExitPointGaps, ReqTrace. `{Project}` below is one of them; the package files sit in `src/{Project}/`.

## Steps

1. Set `VersionPrefix` in `src/{Project}/{Project}.csproj`. A breaking change raises the major version (0.x: the minor). → the new number is in the project file.
2. In `src/{Project}/CHANGELOG.md`, turn the changes into a `## {version}` heading; in the root [`CHANGELOG.md`](../../CHANGELOG.md), list the package and version in the release's package table. Update `src/{Project}/PACKAGE_README.md` when the usage changed. → the three files name the same version.
3. `dotnet build CopilotAIWorkflow.slnx -c Release` → 0 warnings, 0 errors.
4. `dotnet test --solution CopilotAIWorkflow.slnx -c Release --no-build --report-trx --results-directory artifacts/trace-results` → all tests pass, and a new `.trx` file is in `artifacts/trace-results/` (no `--` before the TRX options: [`tech-reqtrace.md`](../../.github/skills/tech-reqtrace.md) § Test evidence).
5. `dotnet run --project src/ReqTrace -c Release --no-build -- check --results artifacts/trace-results` → `0 errors`, exit code 0.
6. `dotnet pack src/{Project}/{Project}.csproj -c Release` → `artifacts/release/{Project}.{version}.nupkg`. This is the only way to a package: it empties the `bin/` output of the project and of every project it references, builds, and packs ([decision record](../decisions/release-pack-path.md)). A build never writes a package.
7. List the package, a zip file: `tar.exe -tf artifacts/release/{Project}.{version}.nupkg` on Windows, `unzip -l artifacts/release/{Project}.{version}.nupkg` on Linux, macOS, or Git Bash (whose `tar` cannot read zip files) → only this package's files. A dotnet tool lists `tools/net10.0/any/` with its own assembly, its dependencies, and `DotnetToolSettings.xml`; nothing named after another or an older project.
8. Push (outward-facing; a version can never be replaced on nuget.org, only unlisted):

   ```bash
   dotnet nuget push artifacts/release/{Project}.{version}.nupkg --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY"
   ```

## Verify

In an empty folder outside the repository, after nuget.org has indexed the package (a few minutes):

```bash
dotnet new tool-manifest
dotnet tool install ExitPointGaps --version {version}      # a tool: ExitPointGaps or ReqTrace
dotnet tool run exitpointgaps --help
```

For a library or the analyzer, add a `PackageReference` with the exact version to a scratch project and build it.

## Troubleshoot

| Symptom | Cause | Fix |
|---------|-------|-----|
| The package holds files of an older or renamed project | The pack did not empty `bin/` first; guarded by `CleanOutputBeforePack` and `CleanToolPublishDir` in [`Directory.Build.targets`](../../Directory.Build.targets) | Pack with the step 6 command only; check that both targets still run; delete `src/{Project}/bin` and `obj`, pack again |
| `{PackageId}: packages are built in Release only` | `dotnet pack -c Debug` (without `-c`, `dotnet pack` already builds Release) | Run the step 6 command |
| `{PackageId}: … --no-build would pack old files` | `dotnet pack --no-build` | Run the step 6 command without `--no-build` |
| `{PackageId}: pack only through the dotnet CLI` | A pack from an IDE or `dotnet msbuild -t:Pack`, which skips the clean | Run the step 6 command in a terminal |
| `MSB3030` "could not be copied" while packing a tool | The tool is packed during build (`GeneratePackageOnBuild`), before publish files exist | Keep `GeneratePackageOnBuild` off for every project ([`Directory.Build.targets`](../../Directory.Build.targets)) |
| A local restore of the version just packed gets old content | `~/.nuget/packages/{id}/{version}` holds an earlier build of the same version | Restore into a private packages folder, as the [sample check](../../samples/CSharpStyleChecker.Demo/README.md) does, or delete that cache folder |
| Push fails with 409 Conflict | The version exists on nuget.org | Raise `VersionPrefix`; never reuse a number |
| Push fails with 403 | The key lacks rights for this package id, or the id belongs to someone else | Use a key scoped to the package; pick a free id before the first release |
