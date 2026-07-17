# ModelReady

One-click preflight checks for Rhino 8 architectural models.

ModelReady is a local, non-destructive Rhino plug-in that turns scattered model-quality checks into one repeatable **Studio Submission** workflow. It scans the active model, groups findings by severity, locates affected objects, and exports a local HTML or JSON report.

## Status

The v0.1 product specification and implementation plan are frozen. The Rhino-independent core engine and Rhino document snapshot adapter are implemented and tested in Rhino 8. The Eto.Forms interface, object location, and report export are later milestones.

- [v0.1 product specification](docs/superpowers/specs/2026-07-17-model-ready-v0.1-design.md)
- [v0.1 implementation plan](docs/superpowers/plans/2026-07-17-model-ready-v0.1.md)
- [中文项目总览](docs/modelready-v0.1-overview.zh-CN.md)
- [scaffold verification evidence](docs/test-evidence/scaffold.md)
- [core preflight engine evidence](docs/test-evidence/core-preflight-engine.md)
- [Rhino snapshot adapter evidence](docs/test-evidence/rhino-snapshot-adapter.md)
- [GitHub Actions workflow template](docs/ci/ci.yml.example)

## v0.1 principles

- Rhino 8.18+ on Windows 10/11
- C# and RhinoCommon, with an Eto.Forms interface
- local-only: no account, cloud service, telemetry, AI, or network dependency
- read-only scanning: no automatic deletion, repair, unlocking, or unhiding
- one built-in profile: **Studio Submission**

## Planned command

Run `ModelReady` in Rhino, confirm the expected model units, and select **Run Preflight**.

The result is one of:

- `NOT READY` — at least one failure must be reviewed
- `READY WITH WARNINGS` — no failures, but warnings remain
- `READY` — no failures or warnings

## Repository state

The core engine provides immutable snapshots, the Studio Submission profile, seven ordered pure rules, readiness calculation, and exception-isolated scan orchestration. It deliberately has no RhinoCommon, filesystem, or UI dependency. The Rhino adapter filters supported top-level model geometry, converts measurements to metres, and supplies exact-duplicate groups to the core engine.

Run the normal build and core tests with:

```powershell
dotnet build ModelReady.sln -c Release -p:BuildYakPackage=false
dotnet test tests/ModelReady.Core.Tests/ModelReady.Core.Tests.csproj -c Release --no-build
```

For the live adapter smoke test, start Rhino's `StartScriptServer`, find its pipe with `rhinocode list`, then run from the repository root:

```powershell
rhinocode --rhino <pipe-id> script tools/integration/CreateFixtureModel.cs
rhinocode --rhino <pipe-id> script tools/integration/RunSnapshotSmokeTest.cs
```

The smoke script loads the Release plug-in assembly, so close that dedicated Rhino test instance before rebuilding the `.rhp` file.

The CI workflow is still stored as a template. A second GitHub CLI authorization attempt on 17 July 2026 timed out, and the active credential still lacks the `workflow` OAuth scope. Moving the template to `.github/workflows/ci.yml` will activate CI after that permission is granted.
