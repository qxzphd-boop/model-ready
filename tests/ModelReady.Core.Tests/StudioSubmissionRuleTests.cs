using ModelReady.Core;

namespace ModelReady.Core.Tests;

public sealed class ExpectedUnitsRuleTests
{
    [Fact]
    public void Matching_units_pass()
    {
        var result = new ExpectedUnitsRule().Evaluate(RuleTestData.Document(unitSystem: "millimeters"), RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-DOC-001", FindingSeverity.Pass);
    }

    [Fact]
    public void Mismatched_units_fail()
    {
        var result = new ExpectedUnitsRule().Evaluate(RuleTestData.Document(unitSystem: "Meters"), RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-DOC-001", FindingSeverity.Fail);
    }
}

public sealed class AbsoluteToleranceRuleTests
{
    [Theory]
    [InlineData(0.000001)]
    [InlineData(0.001)]
    public void Inclusive_tolerance_boundaries_pass(double tolerance)
    {
        var result = new AbsoluteToleranceRule().Evaluate(RuleTestData.Document(tolerance: tolerance), RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-DOC-002", FindingSeverity.Pass);
    }

    [Theory]
    [InlineData(0.0000009)]
    [InlineData(0.0011)]
    public void Tolerance_outside_profile_warns(double tolerance)
    {
        var result = new AbsoluteToleranceRule().Evaluate(RuleTestData.Document(tolerance: tolerance), RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-DOC-002", FindingSeverity.Warning);
    }
}

public sealed class InvalidGeometryRuleTests
{
    [Fact]
    public void Valid_geometry_passes()
    {
        var result = new InvalidGeometryRule().Evaluate(
            RuleTestData.Document(RuleTestData.Object(isValid: true)),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-GEO-001", FindingSeverity.Pass);
    }

    [Fact]
    public void Invalid_geometry_fails_with_affected_object_ids()
    {
        var invalidId = Guid.Parse("f1a4d7aa-e423-456d-8c54-4ea24b3c3050");
        var result = new InvalidGeometryRule().Evaluate(
            RuleTestData.Document(
                RuleTestData.Object(),
                RuleTestData.Object(invalidId, isValid: false)),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-GEO-001", FindingSeverity.Fail, invalidId);
    }
}

public sealed class ExactDuplicatesRuleTests
{
    [Fact]
    public void Empty_and_singleton_duplicate_sets_pass()
    {
        var result = new ExactDuplicatesRule().Evaluate(
            RuleTestData.Document(
                RuleTestData.Object(duplicateSetId: " "),
                RuleTestData.Object(Guid.Parse("c87748d1-a9de-445b-8012-a476560bfef0"), duplicateSetId: "singleton")),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-GEO-002", FindingSeverity.Pass);
    }

    [Fact]
    public void Duplicate_sets_with_two_or_more_objects_warn()
    {
        var first = Guid.Parse("888b648e-c282-4b10-bb68-b54444a58b7f");
        var second = Guid.Parse("7c6c4f25-c517-4af3-a6a4-f9317f750290");
        var result = new ExactDuplicatesRule().Evaluate(
            RuleTestData.Document(
                RuleTestData.Object(first, duplicateSetId: "set-a"),
                RuleTestData.Object(Guid.Parse("0561a8a0-243b-4871-8d4e-923ab52a4142"), duplicateSetId: "singleton"),
                RuleTestData.Object(second, duplicateSetId: "set-a")),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-GEO-002", FindingSeverity.Warning, first, second);
    }
}

public sealed class FarFromOriginRuleTests
{
    [Fact]
    public void Exact_distance_boundary_passes()
    {
        var result = new FarFromOriginRule().Evaluate(
            RuleTestData.Document(RuleTestData.Object(distanceFromOrigin: 1000.0)),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-GEO-003", FindingSeverity.Pass);
    }

    [Fact]
    public void Distance_above_boundary_warns_with_affected_object_ids()
    {
        var objectId = Guid.Parse("6c90ca10-c8c3-48a0-86af-221554bd1022");
        var result = new FarFromOriginRule().Evaluate(
            RuleTestData.Document(RuleTestData.Object(objectId, distanceFromOrigin: 1000.0001)),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-GEO-003", FindingSeverity.Warning, objectId);
    }
}

public sealed class TinyGeometryRuleTests
{
    [Fact]
    public void Exact_diagonal_boundary_passes()
    {
        var result = new TinyGeometryRule().Evaluate(
            RuleTestData.Document(RuleTestData.Object(diagonal: 0.001)),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-GEO-004", FindingSeverity.Pass);
    }

    [Fact]
    public void Diagonal_below_boundary_warns_with_affected_object_ids()
    {
        var objectId = Guid.Parse("526024cd-ac73-471b-b732-0ee858059b11");
        var result = new TinyGeometryRule().Evaluate(
            RuleTestData.Document(RuleTestData.Object(objectId, diagonal: 0.0009)),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-GEO-004", FindingSeverity.Warning, objectId);
    }
}

public sealed class DefaultLayerRuleTests
{
    [Fact]
    public void Geometry_on_named_layers_passes()
    {
        var result = new DefaultLayerRule().Evaluate(
            RuleTestData.Document(RuleTestData.Object(isOnDefaultLayer: false)),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-LAY-001", FindingSeverity.Pass);
    }

    [Fact]
    public void Geometry_on_default_layer_warns_with_affected_object_ids()
    {
        var objectId = Guid.Parse("8b3cc014-a065-4c8d-bae7-c1038d287199");
        var result = new DefaultLayerRule().Evaluate(
            RuleTestData.Document(RuleTestData.Object(objectId, isOnDefaultLayer: true)),
            RuleTestData.Profile());

        RuleTestData.AssertResult(result, "MR-LAY-001", FindingSeverity.Warning, objectId);
    }
}

public sealed class StudioSubmissionRuleSetTests
{
    [Fact]
    public void Rules_are_registered_in_the_frozen_order()
    {
        var ids = StudioSubmissionRuleSet.Create().Select(rule => rule.RuleId);

        Assert.Equal(new[]
        {
            "MR-DOC-001",
            "MR-DOC-002",
            "MR-GEO-001",
            "MR-GEO-002",
            "MR-GEO-003",
            "MR-GEO-004",
            "MR-LAY-001",
        }, ids);
    }
}

internal static class RuleTestData
{
    private static readonly Guid DefaultObjectId = Guid.Parse("7f088510-20ed-4724-af2b-844b34d130e2");

    public static StudioSubmissionProfile Profile()
    {
        return StudioSubmissionProfile.CreateDefault();
    }

    public static DocumentSnapshot Document(
        params ModelObjectSnapshot[] objects)
    {
        return Document("Millimeters", 0.0001, objects);
    }

    public static DocumentSnapshot Document(
        string unitSystem = "Millimeters",
        double tolerance = 0.0001)
    {
        return Document(unitSystem, tolerance, Array.Empty<ModelObjectSnapshot>());
    }

    public static ModelObjectSnapshot Object(
        Guid? objectId = null,
        bool isValid = true,
        bool isOnDefaultLayer = false,
        double diagonal = 1.0,
        double distanceFromOrigin = 0.0,
        string? duplicateSetId = null)
    {
        return new ModelObjectSnapshot(
            objectId ?? DefaultObjectId,
            "Brep",
            isOnDefaultLayer ? "Default" : "Envelope",
            isOnDefaultLayer,
            isValid,
            diagonal,
            distanceFromOrigin,
            duplicateSetId);
    }

    public static void AssertResult(
        RuleResult result,
        string ruleId,
        FindingSeverity severity,
        params Guid[] objectIds)
    {
        Assert.Equal(ruleId, result.RuleId);
        Assert.Equal(severity, result.Severity);
        Assert.Equal(objectIds, result.ObjectIds);
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    private static DocumentSnapshot Document(
        string unitSystem,
        double tolerance,
        IEnumerable<ModelObjectSnapshot> objects)
    {
        return new DocumentSnapshot("Studio model", unitSystem, tolerance, objects);
    }
}
