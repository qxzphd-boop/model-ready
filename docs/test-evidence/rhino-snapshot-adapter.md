# Rhino snapshot adapter verification

Verified on 17 July 2026 with Rhino 8.18.25100.11001 on branch `agent/core-preflight-engine`.

## Implemented scope

- enumerates top-level Curve, Brep, Extrusion, Mesh, and SubD model geometry;
- includes normal, hidden, and locked objects;
- excludes unsupported object types, instance-definition geometry, instance references, reference-model content, deleted objects, and page-space content;
- converts document tolerance, bounding-box diagonal, and centre distance to metres;
- buckets duplicate candidates by object type and tolerance-quantised bounding box;
- calls `GeometryBase.GeometryEquals` only within a candidate bucket;
- assigns stable scan-local duplicate-set IDs in source-object order;
- produces immutable `DocumentSnapshot` values consumed by the core runner.

## Deterministic fixture

`tools/integration/CreateFixtureModel.cs` creates `samples/generated/modelready-v0.1-fixture.3dm` with:

- one box and one hidden exact duplicate;
- one sub-millimetre curve;
- one locked curve more than 1,000 metres from the origin;
- one curve on Rhino's Default layer;
- one unsupported point;
- one block definition and one instance reference;
- millimetre units and 0.01 mm absolute tolerance.

The checked-in `.3dm` is treated as a fixed test asset. The script creates it only when missing; subsequent runs reuse it, and the smoke test validates its contents. This prevents Rhino's regenerated object identifiers and file metadata from making the Git worktree dirty on every verification run.

Fixture result:

```text
PASS|supported_objects=5|excluded_point=1|excluded_block=1|units=Millimeters|tolerance_mm=0.01
```

## Live smoke result

`tools/integration/RunSnapshotSmokeTest.cs` opens the fixture as a headless Rhino document, loads the Release plug-in assembly, creates a snapshot, and runs all seven core rules.

```text
PASS|supported_objects=5|duplicate_groups=1|tiny=1|far=1|default_layer=1|readiness=ReadyWithWarnings
```

## Remaining adapter risks

- Rhino normally rejects adding invalid geometry, so the invalid-geometry rule still needs a separate hand-authored fixture.
- The reference-object exclusion is implemented through both enumerator settings and a defensive object check, but this fixture does not create an attached reference model.
- The 10,000-object/10-second duplicate performance gate remains a later release gate; this milestone verifies the bucketing architecture, not final performance.
