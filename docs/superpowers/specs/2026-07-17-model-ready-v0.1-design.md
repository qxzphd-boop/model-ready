# ModelReady v0.1 Product Specification

**Status:** Frozen for implementation

**Product:** A one-click, local preflight tool for Rhino architectural models

**Target:** Rhino 8.18 or later on Windows 10/11

**Command:** `ModelReady`

## Problem and user

The primary user is an architecture student or designer who has just finished a Rhino model before a pin-up, submission, export, or hand-off. Rhino already exposes individual diagnostic commands, but the user must know which checks matter, remember several commands, interpret each result, and repeat the process while tired and under time pressure.

ModelReady does not judge design quality and does not replace Rhino's geometry engine. It packages a small, explicit submission standard into one repeatable scan, assigns clear severity, locates affected objects, and produces a record of the result.

## Real user flow

1. The user opens the `.3dm` file and runs `ModelReady`.
2. A compact Eto.Forms dialog opens with the built-in **Studio Submission** profile. Expected units default to millimetres and can be changed for the current scan.
3. The user selects **Run Preflight**. The scan is local and does not modify the document.
4. Results are grouped as **Fail**, **Warning**, and **Pass**. Each finding shows a stable rule ID, plain-language explanation, object count, and remediation guidance.
5. For an object finding, the user selects **Locate objects**. ModelReady selects objects it can select and lists the GUID and layer for objects that are hidden or locked. It never unlocks, unhides, deletes, or changes geometry.
6. The user fixes issues manually and reruns the scan.
7. On request, the user exports a self-contained HTML report or machine-readable JSON report. Export never happens automatically.

## Studio Submission rules

| Rule ID | Rule | Severity | Frozen v0.1 behaviour |
|---|---|---:|---|
| `MR-DOC-001` | Expected units | Fail | Current model units must match the units selected in the dialog. Default: millimetres. |
| `MR-DOC-002` | Absolute tolerance | Warning | Convert tolerance to metres and warn when it is outside `0.000001–0.001 m` (`0.001–1 mm`). |
| `MR-GEO-001` | Invalid geometry | Fail | Flag top-level model geometry for which RhinoCommon reports `IsValid == false`. |
| `MR-GEO-002` | Exact duplicate geometry | Warning | Flag exact, co-located duplicate curves, Breps, extrusions, meshes, and SubDs. Candidate bucketing must precede exact comparison. |
| `MR-GEO-003` | Far from origin | Warning | Flag an object's bounding-box centre when its distance from world origin exceeds `1,000 m`. |
| `MR-GEO-004` | Tiny geometry | Warning | Flag supported geometry with a bounding-box diagonal below `0.001 m` (`1 mm`). Points, annotations, lights, and clipping planes are excluded. |
| `MR-LAY-001` | Geometry on Default layer | Warning | Flag supported model geometry whose layer index is Rhino's Default layer. |

The scan includes top-level model-space curves, Breps, extrusions, meshes, and SubDs, including hidden and locked objects. It excludes deleted objects, reference-model content, annotations, lights, clipping planes, layout/detail objects, and geometry nested inside block definitions. Instance references are not expanded in v0.1.

## Readiness result

ModelReady does not calculate a numeric health score.

- **NOT READY:** one or more Fail results
- **READY WITH WARNINGS:** no Fail results and one or more Warning results
- **READY:** no Fail or Warning results

Pass rows remain visible so the user can see which checks actually ran. A scan error is shown separately and cannot be counted as a pass.

## Reports and privacy

Both reports contain schema version, plug-in version, scan time, Rhino version, document name without its full path, selected profile and thresholds, readiness result, per-rule findings, affected object GUIDs, and layer names. Reports contain no telemetry, Rhino account data, Windows username, full filesystem path, or model geometry.

## Acceptance criteria

- A fixture model containing one known example of every rule produces the expected rule IDs, severities, and object counts.
- Re-running after manual correction changes the result without reopening Rhino.
- **Locate objects** selects ordinary affected objects and reports hidden/locked objects without changing their state.
- HTML and JSON reports agree on status and counts and open without internet access.
- Cancelling or encountering an unsupported object leaves the model unchanged.
- A benchmark fixture containing 10,000 supported top-level objects completes within 10 seconds on the development machine; if it does not, v0.1 cannot be called complete.
- Core unit tests, the plug-in build, a packaged `.yak`, and the live Rhino fixture test all pass before release.

## Explicit non-goals

v0.1 will not automatically repair or delete geometry; evaluate architectural quality; check building regulations; inspect Grasshopper definitions; recurse through block definitions; support Revit, BEAM, IFC, BIM metadata, fabrication, or 3D-print profiles; compare model revisions; provide a profile editor; upload reports; use AI or an LLM; provide accounts, collaboration, analytics, or telemetry; promise macOS support; or publish to the public Rhino Package Manager.
