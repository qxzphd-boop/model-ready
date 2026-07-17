# ModelReady

One-click preflight checks for Rhino 8 architectural models.

ModelReady is a local, non-destructive Rhino plug-in that turns scattered model-quality checks into one repeatable **Studio Submission** workflow. It scans the active model, groups findings by severity, locates affected objects, and exports a local HTML or JSON report.

## Status

The v0.1 product specification and implementation plan are frozen. A verified, loadable plug-in scaffold exists; the seven functional preflight rules have not been implemented yet.

- [v0.1 product specification](docs/superpowers/specs/2026-07-17-model-ready-v0.1-design.md)
- [v0.1 implementation plan](docs/superpowers/plans/2026-07-17-model-ready-v0.1.md)
- [中文项目总览](docs/modelready-v0.1-overview.zh-CN.md)
- [scaffold verification evidence](docs/test-evidence/scaffold.md)
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

This repository begins with product and engineering documentation plus a minimal verified plug-in scaffold. Functional code will be implemented milestone-by-milestone with tests and live Rhino verification.

The CI workflow is currently stored as a template because the initial GitHub CLI credential does not have the `workflow` OAuth scope. Moving it to `.github/workflows/ci.yml` will activate it after that permission is granted.
