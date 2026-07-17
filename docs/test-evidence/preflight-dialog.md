# Preflight results dialog verification

Verified on 17 July 2026 with Rhino 8.18.25100.11001 on branch `agent/core-preflight-engine`.

## Implemented scope

- `ModelReady` opens a modal Eto.Forms Studio Submission dialog for the active Rhino document;
- the expected-unit selector supports millimetres, centimetres, metres, inches, and feet;
- `Run Preflight` snapshots the current document and runs the seven core rules in their fixed order;
- readiness, elapsed scan information, finding count, and each rule result update in place;
- readiness is shown with distinct text and colour for Ready, Ready with warnings, Not ready, and scan error;
- the Run button and expected-unit selector are disabled while a scan is executing;
- Locate objects and Export are present but disabled, matching the frozen v0.1 stage boundary;
- pressing Return does not silently run a scan because the dialog deliberately has no default button.

## Automated Rhino smoke result

`tools/integration/RunDialogSmokeTest.cs` creates the real controller and dialog from the Release plug-in assembly. It performs two scans against the deterministic fixture without recreating the controller:

```text
PASS|first=ReadyWithWarnings|second=NotReady|rules=7|locate=false|export=false|dialog=created|default_button=none
```

The first run expects millimetres and returns `ReadyWithWarnings`. The second run expects metres and returns `NotReady`. Both runs return exactly seven ordered rows, and the first row remains `MR-DOC-001`.

## Live Rhino interaction

The dialog was opened from the `ModelReady` command in a dedicated `ModelReadyDev` Rhino scheme with `samples/generated/modelready-v0.1-fixture.3dm` active.

1. With expected units set to `Millimeters`, the same dialog returned `READY WITH WARNINGS`, reported five supported objects, and showed seven rows.
2. The selector was changed to `Meters` without closing the dialog.
3. A second click on `Run Preflight` changed the status to `NOT READY`; `MR-DOC-001` changed from Pass to Fail with `Document units are Millimeters; expected Meters.`
4. The other six rows continued to return normally, and Locate objects and Export remained disabled.

This interaction also exposed two pre-release UI defects: Return initially triggered the default button, and the Run button could be clipped by the header layout. A regression smoke assertion now requires a null default button, and the header uses a bounded nested layout. The corrected dialog passed the full two-run interaction again.

## Reproduction

Build the Release plug-in, start a dedicated Rhino instance with `StartScriptServer`, locate its pipe with `rhinocode list`, and run:

```powershell
rhinocode --rhino <pipe-id> script tools/integration/CreateFixtureModel.cs
rhinocode --rhino <pipe-id> script tools/integration/RunDialogSmokeTest.cs
Get-Content samples/generated/dialog-smoke-status.txt
```

Then open the fixture in Rhino, run `ModelReady`, and repeat the two expected-unit scans described above.

## Explicitly deferred

This milestone does not locate or select affected objects, write HTML or JSON reports, run scans in a background thread, publish a Yak package, or implement the 10,000-object performance gate.
