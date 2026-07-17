# Core preflight engine verification

Verified on 17 July 2026 on branch `agent/core-preflight-engine`.

## Implemented scope

- immutable Rhino-independent document, object, profile, result, error, and report models;
- readiness precedence: Fail, then Warning, then Pass;
- seven Studio Submission rules in the frozen order;
- inclusive tolerance, origin-distance, and tiny-geometry boundaries;
- duplicate findings based only on adapter-supplied `DuplicateSetId` values;
- per-rule exception isolation with readiness suppressed whenever a scan error occurs.

## Verification commands

```powershell
dotnet restore ModelReady.sln
dotnet build ModelReady.sln -c Release --no-restore -p:BuildYakPackage=false
dotnet test tests/ModelReady.Core.Tests/ModelReady.Core.Tests.csproj -c Release --no-build
git diff --check
```

The local verification completed with zero build warnings, zero build errors, and 40 passing tests. The solution build also compiled `ModelReady.Rhino`, proving that the existing plug-in scaffold remains compatible with the new core assembly.

## CI authorization status

`gh auth refresh -h github.com -s workflow --clipboard` was attempted once during this milestone. The authorization window timed out, and `gh auth status` still reported only `gist`, `read:org`, and `repo`. Therefore `docs/ci/ci.yml.example` remains a non-active template; no workflow file was added under `.github/workflows`.

## Explicitly deferred

This milestone does not create Rhino snapshots, compare Rhino geometry, build an Eto.Forms interface, locate objects, write HTML/JSON reports, run the 10,000-object performance gate, or publish a Yak package.
