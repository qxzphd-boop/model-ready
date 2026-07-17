# ModelReady v0.1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a non-destructive Rhino 8 plug-in that runs the frozen Studio Submission checks, locates affected objects, and exports matching local HTML and JSON reports.

**Architecture:** Keep all status calculation and rule evaluation in a Rhino-independent `ModelReady.Core` library. `ModelReady.Rhino` is a thin adapter that snapshots the active `RhinoDoc`, performs Rhino-specific geometry comparisons, displays an Eto.Forms dialog, selects objects, and writes reports. This split makes most behaviour testable without launching Rhino while preserving a local live-Rhino integration gate.

**Tech Stack:** C#; .NET Standard 2.0 for the core library; .NET 7 for the Rhino plug-in; .NET 10 for the test runner and CI SDK; RhinoCommon `8.18.25100.11001`; Eto.Forms supplied by Rhino; System.Text.Json; xUnit; GitHub Actions; RhinoCode CLI; Yak packaging.

## Global Constraints

- Target Rhino 8.18 or later on Windows 10/11; do not claim macOS support in v0.1.
- Command name is exactly `ModelReady`.
- Scanning is read-only and local; no automatic fix, delete, unlock, unhide, upload, network call, telemetry, AI, or LLM.
- Implement only the seven rules and object scope frozen in the product specification.
- Use `NOT READY`, `READY WITH WARNINGS`, and `READY`; do not add a numeric health score.
- Pin RhinoCommon to `8.18.25100.11001`, matching the development machine's installed Rhino.
- Do not publish to the public Rhino Package Manager in v0.1; build a local Yak package only.
- Do not add dependencies beyond RhinoCommon and the test/build packages unless a documented blocker proves they are necessary.

---

## Technical decision and alternatives

### Selected: C# + RhinoCommon

C# is the v0.1 product language because Rhino's official plug-in template creates a compiled `.rhp`, RhinoCommon is a typed .NET API, Eto.Forms is the supported cross-platform UI toolkit, and the build can produce a Yak package. It also makes module boundaries, interfaces, tests, and error handling clearer in a public software-engineering repository.

### Rejected for the product path: Rhino Python 3 script plug-in

Rhino 8 can publish Python 3 scripts as `.rhp`/Yak packages, so Python is technically feasible and remains useful for isolated API experiments. It is not selected because ModelReady needs a durable dialog, multiple services, structured domain types, repeatable builds, and clear compile-time API feedback. Reconsider Python only if a short C# spike cannot load, run, and debug on the development machine.

### Rejected: external web application or Rhino.Compute

ModelReady operates on the open document and must work during a deadline without a server. A web front end, database, cloud API, account system, and Rhino.Compute add operational cost without helping the v0.1 user flow.

## Planned repository structure

```text
model-ready/
├── .github/workflows/ci.yml
├── docs/superpowers/specs/2026-07-17-model-ready-v0.1-design.md
├── docs/superpowers/plans/2026-07-17-model-ready-v0.1.md
├── samples/README.md
├── src/
│   ├── ModelReady.Core/
│   │   ├── Checks/
│   │   ├── Models/
│   │   ├── Profiles/
│   │   └── ModelReady.Core.csproj
│   └── ModelReady.Rhino/
│       ├── Commands/ModelReadyCommand.cs
│       ├── Scanning/
│       ├── Selection/
│       ├── Reporting/
│       ├── UI/
│       ├── Resources/studio-submission.json
│       ├── ModelReadyPlugin.cs
│       └── ModelReady.Rhino.csproj
├── tests/ModelReady.Core.Tests/
├── tools/integration/
├── Directory.Build.props
├── ModelReady.sln
└── README.md
```

## Frozen interfaces

The implementation must preserve these names so tasks can be developed independently:

```csharp
public enum FindingSeverity { Pass, Warning, Fail }
public enum ReadinessStatus { Ready, ReadyWithWarnings, NotReady }

public sealed class ModelObjectSnapshot
{
    public Guid ObjectId { get; init; }
    public string GeometryType { get; init; } = "";
    public string LayerName { get; init; } = "";
    public bool IsOnDefaultLayer { get; init; }
    public bool IsValid { get; init; }
    public double BoundingBoxDiagonalMetres { get; init; }
    public double BoundingBoxCentreDistanceMetres { get; init; }
    public string? DuplicateSetId { get; init; }
}

public sealed class DocumentSnapshot
{
    public string DocumentName { get; init; } = "Untitled";
    public string UnitSystem { get; init; } = "None";
    public double AbsoluteToleranceMetres { get; init; }
    public IReadOnlyList<ModelObjectSnapshot> Objects { get; init; }
        = Array.Empty<ModelObjectSnapshot>();
}

public sealed class StudioSubmissionProfile
{
    public string ExpectedUnitSystem { get; init; } = "Millimeters";
    public double MinToleranceMetres { get; init; } = 0.000001;
    public double MaxToleranceMetres { get; init; } = 0.001;
    public double FarFromOriginMetres { get; init; } = 1000.0;
    public double TinyGeometryMetres { get; init; } = 0.001;
}

public sealed class RuleResult
{
    public string RuleId { get; init; } = "";
    public string Title { get; init; } = "";
    public FindingSeverity Severity { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyList<Guid> ObjectIds { get; init; } = Array.Empty<Guid>();
}

public interface IModelReadyRule
{
    string RuleId { get; }
    RuleResult Evaluate(DocumentSnapshot document, StudioSubmissionProfile profile);
}

public interface IRhinoDocumentSnapshotFactory
{
    DocumentSnapshot Create(Rhino.RhinoDoc document);
}
```

## Risks and planned solutions

| Risk | Why it matters | Planned solution and decision gate |
|---|---|---|
| Rhino/API version mismatch | A plug-in can compile against a newer RhinoCommon and fail on the installed Rhino. | Pin RhinoCommon to the installed `8.18.25100.11001`; load the boilerplate in Rhino before implementing rules. Upgrade only with a separate compatibility decision. |
| Most CI runners do not have licensed Rhino | A green GitHub workflow cannot prove the plug-in works inside Rhino. | CI builds the `.rhp` and runs core tests. A local RhinoCode fixture run is a mandatory release gate and its output is saved as an artifact. |
| False positives | Open or tiny geometry may be intentional; excessive warnings make the tool useless. | v0.1 omits generic open-geometry checks, uses Warning rather than Fail for ambiguous rules, exposes thresholds in the report, and never auto-fixes. Validate on three real student models before release. |
| Duplicate scan performance | Pairwise comparison is `O(n²)` and can freeze a large model. | Bucket candidates by geometry type and tolerance-quantised bounding box, then call `GeometryBase.GeometryEquals` only within a bucket. The 10,000-object/10-second acceptance gate blocks release. |
| Rhino API thread safety and UI freezes | Moving geometry work to background threads can be unsafe; keeping it on the UI thread can block interaction. | Keep Rhino object access on the Rhino command thread in v0.1, minimise work via snapshots and bucketing, show elapsed progress, and optimise before introducing concurrency. Do not add unsafe parallel geometry calls. |
| Hidden or locked findings cannot be visibly selected | Silently changing visibility or locking would violate non-destructive behaviour. | Attempt normal selection only; list GUID and layer for objects Rhino will not select. Never unhide or unlock. |
| Unit-dependent thresholds | A value meaningful in millimetres is wrong in metres. | Convert all measurements to metres in the snapshot using Rhino unit conversion, then evaluate profile thresholds in metres. |
| Blocks and linked references | Recursion and reference ownership introduce complex identity and modification rules. | Exclude nested block-definition and reference-model geometry in v0.1 and state the exclusion in UI/report. |
| Report privacy | Absolute paths can expose the user's name or project locations. | Store document basename only; exclude paths, usernames, geometry, account data, and machine identifiers. |
| Distribution complexity | A build can work locally but be hard for another user to install. | First validate direct `.rhp` loading, then build a local Yak with the Rhino template/Yak tooling. Do not publish to the public server before external testing. |
| User cannot explain generated code | The repository would be weak evidence if its owner cannot discuss it. | Each milestone ends with a short architecture note and one manual walkthrough: inputs, algorithm, test, and failure mode. No milestone is accepted solely because Codex generated compiling code. |

---

### Task 1: Establish a loadable plug-in skeleton and CI baseline

**Files:**
- Create: `ModelReady.sln`
- Create: `Directory.Build.props`
- Create: `src/ModelReady.Core/ModelReady.Core.csproj`
- Create: `src/ModelReady.Rhino/ModelReady.Rhino.csproj`
- Create: `src/ModelReady.Rhino/ModelReadyPlugin.cs`
- Create: `src/ModelReady.Rhino/Commands/ModelReadyCommand.cs`
- Create: `tests/ModelReady.Core.Tests/ModelReady.Core.Tests.csproj`
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: a `ModelReady` Rhino command and a solution that restores, builds, and tests from the command line.

- [ ] **Step 1: Create the solution and three projects**

Run:

```powershell
dotnet new sln -n ModelReady
dotnet new classlib -n ModelReady.Core -o src/ModelReady.Core -f netstandard2.0
dotnet new classlib -n ModelReady.Rhino -o src/ModelReady.Rhino -f net7.0
dotnet new xunit -n ModelReady.Core.Tests -o tests/ModelReady.Core.Tests -f net10.0
dotnet sln ModelReady.sln add src/ModelReady.Core/ModelReady.Core.csproj
dotnet sln ModelReady.sln add src/ModelReady.Rhino/ModelReady.Rhino.csproj
dotnet sln ModelReady.sln add tests/ModelReady.Core.Tests/ModelReady.Core.Tests.csproj
dotnet add src/ModelReady.Rhino/ModelReady.Rhino.csproj reference src/ModelReady.Core/ModelReady.Core.csproj
dotnet add tests/ModelReady.Core.Tests/ModelReady.Core.Tests.csproj reference src/ModelReady.Core/ModelReady.Core.csproj
```

Expected: all three projects appear in `dotnet sln ModelReady.sln list`.

- [ ] **Step 2: Configure the Rhino plug-in project**

Set `ModelReady.Rhino.csproj` to:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <TargetExt>.rhp</TargetExt>
    <AssemblyName>ModelReady</AssemblyName>
    <RootNamespace>ModelReady.Rhino</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ModelReady.Core\ModelReady.Core.csproj" />
    <PackageReference Include="RhinoCommon" Version="8.18.25100.11001" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add a minimal command and plug-in identity**

Create `ModelReadyCommand` inheriting `Rhino.Commands.Command`, return `ModelReady` from `EnglishName`, write `ModelReady v0.1 scaffold loaded.` to the Rhino command line, and return `Result.Success`. Create one stable plug-in GUID and never regenerate it.

- [ ] **Step 4: Verify restore, build, and unit-test discovery**

Run:

```powershell
dotnet restore ModelReady.sln
dotnet build ModelReady.sln -c Release --no-restore
dotnet test ModelReady.sln -c Release --no-build
```

Expected: build succeeds, the output contains `ModelReady.rhp`, and the generated xUnit sample test passes.

- [ ] **Step 5: Load the plug-in in Rhino 8.18**

Install the Release `.rhp` through Rhino Plug-in Manager, run `ModelReady`, and confirm the exact scaffold message appears. Save the command output to `artifacts/manual/task-01-rhino-load.txt`.

- [ ] **Step 6: Commit**

```powershell
git add ModelReady.sln Directory.Build.props src tests .github
git commit -m "build: add Rhino plugin skeleton"
```

---

### Task 2: Define the Rhino-independent domain model and readiness calculation

**Files:**
- Create: `src/ModelReady.Core/Models/FindingSeverity.cs`
- Create: `src/ModelReady.Core/Models/ReadinessStatus.cs`
- Create: `src/ModelReady.Core/Models/ModelObjectSnapshot.cs`
- Create: `src/ModelReady.Core/Models/DocumentSnapshot.cs`
- Create: `src/ModelReady.Core/Models/RuleResult.cs`
- Create: `src/ModelReady.Core/Checks/IModelReadyRule.cs`
- Create: `src/ModelReady.Core/Checks/ReadinessCalculator.cs`
- Test: `tests/ModelReady.Core.Tests/Checks/ReadinessCalculatorTests.cs`

**Interfaces:**
- Produces: the frozen interfaces above and `ReadinessStatus ReadinessCalculator.Calculate(IEnumerable<RuleResult> results)`.

- [ ] **Step 1: Write failing readiness tests**

Cover exactly these cases: any Fail returns `NotReady`; no Fail plus one Warning returns `ReadyWithWarnings`; Pass-only returns `Ready`; an empty result set throws `InvalidOperationException` because a scan with no executed rules cannot be ready.

- [ ] **Step 2: Run the focused test and confirm failure**

```powershell
dotnet test tests/ModelReady.Core.Tests --filter FullyQualifiedName~ReadinessCalculatorTests
```

Expected: FAIL because `ReadinessCalculator` does not exist.

- [ ] **Step 3: Implement the frozen domain types and minimal calculator**

Use an explicit precedence of Fail, then Warning, then Pass. Reject null and empty collections. Do not introduce numeric scores.

- [ ] **Step 4: Re-run focused and complete tests**

Expected: all readiness tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/ModelReady.Core tests/ModelReady.Core.Tests
git commit -m "feat: add preflight result model"
```

---

### Task 3: Implement the Studio Submission profile and seven pure rules

**Files:**
- Create: `src/ModelReady.Core/Profiles/StudioSubmissionProfile.cs`
- Create: `src/ModelReady.Core/Checks/ExpectedUnitsRule.cs`
- Create: `src/ModelReady.Core/Checks/AbsoluteToleranceRule.cs`
- Create: `src/ModelReady.Core/Checks/InvalidGeometryRule.cs`
- Create: `src/ModelReady.Core/Checks/DuplicateGeometryRule.cs`
- Create: `src/ModelReady.Core/Checks/FarFromOriginRule.cs`
- Create: `src/ModelReady.Core/Checks/TinyGeometryRule.cs`
- Create: `src/ModelReady.Core/Checks/DefaultLayerRule.cs`
- Test: `tests/ModelReady.Core.Tests/Checks/*RuleTests.cs`

**Interfaces:**
- Consumes: `DocumentSnapshot`, `ModelObjectSnapshot`, and `StudioSubmissionProfile`.
- Produces: one `IModelReadyRule` per frozen rule ID.

- [ ] **Step 1: Write one failing test class per rule**

Each test class must cover pass and finding cases, exact rule ID, exact severity, and affected GUIDs. Boundary tests are mandatory: tolerance exactly at each limit passes; origin distance exactly `1000.0` passes; tiny diagonal exactly `0.001` passes.

- [ ] **Step 2: Run the rule test folder and confirm failure**

```powershell
dotnet test tests/ModelReady.Core.Tests --filter FullyQualifiedName~RuleTests
```

- [ ] **Step 3: Implement minimal rule classes**

Rules evaluate snapshot values only. They must not reference RhinoCommon, filesystem APIs, UI types, or network APIs. Duplicate groups are represented by non-empty `DuplicateSetId`; only groups with at least two object IDs become a finding.

- [ ] **Step 4: Run complete core tests**

Expected: all domain and rule tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/ModelReady.Core tests/ModelReady.Core.Tests
git commit -m "feat: add studio submission rules"
```

---

### Task 4: Build the Rhino document snapshot and duplicate detector

**Files:**
- Create: `src/ModelReady.Rhino/Scanning/IRhinoDocumentSnapshotFactory.cs`
- Create: `src/ModelReady.Rhino/Scanning/RhinoDocumentSnapshotFactory.cs`
- Create: `src/ModelReady.Rhino/Scanning/DuplicateCandidateKey.cs`
- Create: `src/ModelReady.Rhino/Scanning/RhinoDuplicateDetector.cs`
- Create: `tools/integration/CreateFixtureModel.cs`
- Create: `tools/integration/RunSnapshotSmokeTest.cs`

**Interfaces:**
- Produces: `DocumentSnapshot Create(RhinoDoc document)` and `IReadOnlyDictionary<Guid,string> FindDuplicateSets(IReadOnlyList<RhinoObject> objects, double tolerance)`.

- [ ] **Step 1: Create a deterministic Rhino fixture**

The fixture script must create: one valid box; one exact duplicate box; one tiny curve; one far-from-origin curve; one curve on Default layer; and a named non-default layer. Save as `samples/generated/modelready-v0.1-fixture.3dm`. Invalid geometry may need a separate hand-authored fixture because Rhino can reject adding it.

- [ ] **Step 2: Implement object filtering and unit normalisation**

Enumerate top-level model-space Curve, Brep, Extrusion, Mesh, and SubD objects. Include normal, hidden, and locked objects. Exclude reference objects and instance-definition internals. Convert tolerance, bounding-box diagonal, and centre distance to metres before creating snapshots.

- [ ] **Step 3: Implement duplicate bucketing**

Build `DuplicateCandidateKey` from geometry type plus bounding-box min/max coordinates quantised by document tolerance. Within each bucket, use `GeometryBase.GeometryEquals` to form exact duplicate sets. Never compare every object with every other object.

- [ ] **Step 4: Run the snapshot smoke test inside Rhino**

Start Rhino's script server with `StartScriptServer`, then run the fixture and smoke-test scripts through:

```powershell
& 'C:\Program Files\Rhino 8\System\rhinocode.exe' script tools\integration\CreateFixtureModel.cs
& 'C:\Program Files\Rhino 8\System\rhinocode.exe' script tools\integration\RunSnapshotSmokeTest.cs
```

Expected: the smoke test prints the expected supported object count, metre-normalised values, and one duplicate group.

- [ ] **Step 5: Commit**

```powershell
git add src/ModelReady.Rhino/Scanning tools/integration samples
git commit -m "feat: snapshot Rhino model data"
```

---

### Task 5: Orchestrate scans and handle errors without false passes

**Files:**
- Create: `src/ModelReady.Core/Checks/PreflightRunner.cs`
- Create: `src/ModelReady.Core/Models/PreflightReport.cs`
- Test: `tests/ModelReady.Core.Tests/Checks/PreflightRunnerTests.cs`

**Interfaces:**
- Produces: `PreflightReport Run(DocumentSnapshot document, StudioSubmissionProfile profile)` containing all seven rule results, elapsed duration, and readiness.

- [ ] **Step 1: Write failing orchestration tests**

Verify stable rule order, exactly seven results, readiness precedence, and that an exception from one rule produces a scan error rather than a Pass.

- [ ] **Step 2: Implement the runner with explicit rule registration**

Register rules in ID order. Do not discover rules by reflection. Store scan errors separately from `RuleResult` and refuse to calculate `Ready` while errors exist.

- [ ] **Step 3: Run all core tests and commit**

```powershell
dotnet test tests/ModelReady.Core.Tests -c Release
git add src/ModelReady.Core tests/ModelReady.Core.Tests
git commit -m "feat: orchestrate model preflight"
```

---

### Task 6: Add the Eto.Forms scan and result dialog

**Files:**
- Create: `src/ModelReady.Rhino/UI/ModelReadyDialog.cs`
- Create: `src/ModelReady.Rhino/UI/RuleResultViewModel.cs`
- Modify: `src/ModelReady.Rhino/Commands/ModelReadyCommand.cs`

**Interfaces:**
- Consumes: snapshot factory and `PreflightRunner`.
- Produces: profile controls, Run Preflight action, readiness banner, grouped result grid, Locate objects action, and Export action.

- [ ] **Step 1: Build the smallest dialog shell**

Use Eto.Forms controls only. Show profile name, expected-unit dropdown, Run button, empty result grid, and disabled Locate/Export buttons. Do not add a dockable panel, custom theme, animation, or settings page.

- [ ] **Step 2: Bind the scan flow**

Disable Run while scanning, display elapsed time, and restore the button on success or error. Show `NOT READY`, `READY WITH WARNINGS`, or `READY` using text plus colour; colour must never be the only status signal.

- [ ] **Step 3: Run a live interaction check**

Open the fixture, run `ModelReady`, change expected units, run the scan twice, and confirm the status and counts update without reopening Rhino.

- [ ] **Step 4: Commit**

```powershell
git add src/ModelReady.Rhino/UI src/ModelReady.Rhino/Commands
git commit -m "feat: add preflight results dialog"
```

---

### Task 7: Locate affected objects without modifying visibility or locks

**Files:**
- Create: `src/ModelReady.Rhino/Selection/ResultSelectionService.cs`
- Create: `src/ModelReady.Rhino/Selection/SelectionOutcome.cs`
- Modify: `src/ModelReady.Rhino/UI/ModelReadyDialog.cs`

**Interfaces:**
- Produces: `SelectionOutcome Select(RhinoDoc document, IReadOnlyList<Guid> objectIds)` with selected, hidden, locked, and missing IDs.

- [ ] **Step 1: Implement classification before selection**

Look up each object ID and classify missing, hidden, locked, or ordinarily selectable. Select only ordinary objects, call `doc.Views.Redraw()`, and return every unselected ID with a reason. Do not call Show, Unlock, Delete, Purge, or any geometry mutation.

- [ ] **Step 2: Display partial-selection outcomes**

The dialog must say, for example, `Selected 3 objects; 1 hidden; 2 locked`, and list GUID plus layer for unselected items.

- [ ] **Step 3: Validate document immutability**

Capture object count, layer count, hidden state, locked state, and document modified flag before and after Locate. They must remain unchanged except for selection state.

- [ ] **Step 4: Commit**

```powershell
git add src/ModelReady.Rhino/Selection src/ModelReady.Rhino/UI
git commit -m "feat: locate preflight findings safely"
```

---

### Task 8: Export privacy-safe JSON and self-contained HTML

**Files:**
- Create: `src/ModelReady.Rhino/Reporting/ReportDocument.cs`
- Create: `src/ModelReady.Rhino/Reporting/JsonReportWriter.cs`
- Create: `src/ModelReady.Rhino/Reporting/HtmlReportWriter.cs`
- Create: `src/ModelReady.Rhino/Reporting/ReportPathService.cs`
- Test: `tests/ModelReady.Core.Tests/Reporting/ReportContractTests.cs`

**Interfaces:**
- Produces: `string WriteJson(PreflightReport report)` and `string WriteHtml(PreflightReport report)` with equivalent counts and status.

- [ ] **Step 1: Freeze report schema `1.0` with contract tests**

Assert schema version, plug-in version, scan time, Rhino version, document basename, profile thresholds, readiness, rule IDs, counts, object GUIDs, and layer names. Assert that test output contains no `C:\Users\`, username, source path, geometry coordinates, or account data.

- [ ] **Step 2: Implement JSON with System.Text.Json**

Use indented UTF-8 JSON and stable property names. Do not serialise RhinoCommon objects.

- [ ] **Step 3: Implement self-contained HTML**

Embed CSS and escaped content in one HTML file. Use semantic headings and a table; do not load fonts, scripts, analytics, or assets from the network.

- [ ] **Step 4: Add explicit export UI**

Open a Save File dialog only after the user selects Export. Never write reports on scan. If the user cancels, create no file.

- [ ] **Step 5: Compare report outputs and commit**

```powershell
dotnet test tests/ModelReady.Core.Tests -c Release
git add src tests
git commit -m "feat: export local preflight reports"
```

---

### Task 9: Performance, fixture, and non-destructive release gates

**Files:**
- Create: `tools/integration/CreateBenchmarkModel.cs`
- Create: `tools/integration/RunV01Acceptance.cs`
- Create: `samples/README.md`
- Create: `docs/test-evidence/v0.1.md`

**Interfaces:**
- Produces: repeatable local evidence for functional counts, 10,000-object performance, report parity, and document immutability.

- [ ] **Step 1: Generate deterministic acceptance fixtures**

Create one rule fixture and one 10,000-object benchmark using fixed inputs. Do not commit private student models; use three real models only for manual false-positive review and record anonymised counts.

- [ ] **Step 2: Run the complete acceptance script**

The script must fail with a non-zero exit result when rule counts differ, status differs, reports disagree, the document is modified, or runtime exceeds 10 seconds.

- [ ] **Step 3: Record evidence**

Write Rhino version, plug-in commit, fixture checksum, elapsed time, result counts, and report parity to `docs/test-evidence/v0.1.md`.

- [ ] **Step 4: Commit**

```powershell
git add tools samples docs/test-evidence
git commit -m "test: add Rhino acceptance fixtures"
```

---

### Task 10: Package locally and finish public documentation

**Files:**
- Create: `manifest.yml`
- Create: `CHANGELOG.md`
- Modify: `README.md`
- Modify: `src/ModelReady.Rhino/ModelReady.Rhino.csproj`

**Interfaces:**
- Produces: install instructions, usage screenshots, limitations, and a local `modelready-0.1.0-rh8-win.yak` package.

- [ ] **Step 1: Build Release and local Yak package**

Use Rhino's installed Yak CLI after the `.rhp` passes live testing:

```powershell
dotnet build ModelReady.sln -c Release
& 'C:\Program Files\Rhino 8\System\yak.exe' build --platform win
```

Expected: a Rhino 8 Windows `.yak` package is created under `dist/`; it remains ignored by Git.

- [ ] **Step 2: Install the package on the development machine**

Remove the directly loaded development plug-in registration, install the local Yak through Rhino Package Manager, restart Rhino, run `ModelReady`, and repeat the rule fixture scan.

- [ ] **Step 3: Finish README and changelog**

Document install, command, user flow, seven rules, report privacy, current limitations, development commands, testing evidence, and explicit exclusions. Include one screenshot of the result dialog and one redacted HTML report screenshot.

- [ ] **Step 4: Run final verification**

```powershell
dotnet restore ModelReady.sln
dotnet build ModelReady.sln -c Release --no-restore
dotnet test ModelReady.sln -c Release --no-build
git status --short
```

Expected: restore/build/test pass and only intentionally uncommitted screenshot/evidence changes remain.

- [ ] **Step 5: Commit**

```powershell
git add README.md CHANGELOG.md manifest.yml src/ModelReady.Rhino docs/test-evidence
git commit -m "docs: prepare ModelReady v0.1"
```

## Official technical sources

- [RhinoCommon plug-in guide for Windows](https://developer.rhino3d.com/guides/rhinocommon/your-first-plugin-windows/)
- [Rhino 8 Script Editor project creation](https://developer.rhino3d.com/guides/scripting/projects-create/)
- [Rhino 8 Script Editor project publishing](https://developer.rhino3d.com/guides/scripting/projects-publish/)
- [RhinoCode command-line interface](https://developer.rhino3d.com/guides/scripting/advanced-cli/)
- [Rhino Package Manager overview](https://developer.rhino3d.com/guides/yak/what-is-yak/)
- [Creating a Rhino plug-in Yak package](https://developer.rhino3d.com/guides/yak/creating-a-rhino-plugin-package/)
- [McNeel Rhino developer samples](https://github.com/mcneel/rhino-developer-samples)

## Plan self-review result

- **Specification coverage:** all seven rules, object scope, three readiness states, non-destructive selection, two report formats, performance, local integration, packaging, and non-goals have an implementation task.
- **Placeholder scan:** no implementation requirement is left as TBD or TODO.
- **Type consistency:** the plan uses the frozen domain and adapter interface names throughout.
- **Scope check:** v0.1 remains one Rhino plug-in with one profile; fabrication, BIM exchange, automatic repair, block recursion, cloud services, and public package publication remain excluded.
