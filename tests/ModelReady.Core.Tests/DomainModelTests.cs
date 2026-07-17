using ModelReady.Core;

namespace ModelReady.Core.Tests;

public sealed class DomainModelTests
{
    private static readonly Guid ObjectId = Guid.Parse("7f088510-20ed-4724-af2b-844b34d130e2");

    [Fact]
    public void Model_object_snapshot_preserves_valid_values()
    {
        var snapshot = CreateObject(duplicateSetId: "duplicate-a");

        Assert.Equal(ObjectId, snapshot.ObjectId);
        Assert.Equal("Brep", snapshot.GeometryType);
        Assert.Equal("Envelope", snapshot.LayerName);
        Assert.True(snapshot.IsValid);
        Assert.False(snapshot.IsOnDefaultLayer);
        Assert.Equal(0.5, snapshot.BoundingBoxDiagonalMetres);
        Assert.Equal(25.0, snapshot.BoundingBoxCentreDistanceMetres);
        Assert.Equal("duplicate-a", snapshot.DuplicateSetId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Model_object_snapshot_normalises_missing_duplicate_set_ids(string? duplicateSetId)
    {
        var snapshot = CreateObject(duplicateSetId: duplicateSetId);

        Assert.Null(snapshot.DuplicateSetId);
    }

    [Fact]
    public void Model_object_snapshot_rejects_invalid_identity_and_text()
    {
        Assert.Throws<ArgumentException>(() => new ModelObjectSnapshot(
            Guid.Empty, "Brep", "Envelope", false, true, 1, 1, null));
        Assert.Throws<ArgumentException>(() => new ModelObjectSnapshot(
            ObjectId, " ", "Envelope", false, true, 1, 1, null));
        Assert.Throws<ArgumentNullException>(() => new ModelObjectSnapshot(
            ObjectId, null!, "Envelope", false, true, 1, 1, null));
        Assert.Throws<ArgumentException>(() => new ModelObjectSnapshot(
            ObjectId, "Brep", " ", false, true, 1, 1, null));
        Assert.Throws<ArgumentNullException>(() => new ModelObjectSnapshot(
            ObjectId, "Brep", null!, false, true, 1, 1, null));
    }

    [Fact]
    public void Model_object_snapshot_rejects_negative_or_non_finite_measurements()
    {
        foreach (var value in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ModelObjectSnapshot(
                ObjectId, "Brep", "Envelope", false, true, value, 1, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ModelObjectSnapshot(
                ObjectId, "Brep", "Envelope", false, true, 1, value, null));
        }
    }

    [Fact]
    public void Document_snapshot_copies_the_object_collection()
    {
        var source = new List<ModelObjectSnapshot> { CreateObject() };
        var snapshot = new DocumentSnapshot("Studio model", "Millimeters", 0.0001, source);

        source.Clear();

        Assert.Single(snapshot.Objects);
        Assert.Equal("Studio model", snapshot.DocumentName);
        Assert.Equal("Millimeters", snapshot.UnitSystem);
        Assert.Equal(0.0001, snapshot.AbsoluteToleranceMetres);
    }

    [Fact]
    public void Document_snapshot_rejects_invalid_input()
    {
        Assert.Throws<ArgumentException>(() => new DocumentSnapshot(" ", "Millimeters", 0.0001, Array.Empty<ModelObjectSnapshot>()));
        Assert.Throws<ArgumentNullException>(() => new DocumentSnapshot(null!, "Millimeters", 0.0001, Array.Empty<ModelObjectSnapshot>()));
        Assert.Throws<ArgumentException>(() => new DocumentSnapshot("Model", " ", 0.0001, Array.Empty<ModelObjectSnapshot>()));
        Assert.Throws<ArgumentNullException>(() => new DocumentSnapshot("Model", null!, 0.0001, Array.Empty<ModelObjectSnapshot>()));
        Assert.Throws<ArgumentNullException>(() => new DocumentSnapshot("Model", "Millimeters", 0.0001, null!));
        Assert.Throws<ArgumentException>(() => new DocumentSnapshot("Model", "Millimeters", 0.0001, new ModelObjectSnapshot[] { null! }));

        foreach (var value in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentSnapshot(
                "Model", "Millimeters", value, Array.Empty<ModelObjectSnapshot>()));
        }
    }

    [Fact]
    public void Default_studio_submission_profile_uses_frozen_thresholds()
    {
        var profile = StudioSubmissionProfile.CreateDefault();

        Assert.Equal("Studio Submission", profile.Name);
        Assert.Equal("Millimeters", profile.ExpectedUnitSystem);
        Assert.Equal(0.000001, profile.MinToleranceMetres);
        Assert.Equal(0.001, profile.MaxToleranceMetres);
        Assert.Equal(1000.0, profile.FarFromOriginMetres);
        Assert.Equal(0.001, profile.TinyGeometryMetres);
    }

    [Fact]
    public void Studio_submission_profile_rejects_invalid_text_ranges_and_measurements()
    {
        Assert.Throws<ArgumentException>(() => CreateProfile(name: " "));
        Assert.Throws<ArgumentNullException>(() => CreateProfile(name: null!));
        Assert.Throws<ArgumentException>(() => CreateProfile(expectedUnits: " "));
        Assert.Throws<ArgumentNullException>(() => CreateProfile(expectedUnits: null!));
        Assert.Throws<ArgumentException>(() => CreateProfile(minTolerance: 0.002, maxTolerance: 0.001));

        foreach (var value in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateProfile(minTolerance: value));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateProfile(maxTolerance: value));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateProfile(farFromOrigin: value));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateProfile(tinyGeometry: value));
        }
    }

    [Fact]
    public void Rule_result_copies_object_ids_and_rejects_invalid_input()
    {
        var ids = new List<Guid> { ObjectId };
        var result = new RuleResult("MR-TEST-001", "Test rule", FindingSeverity.Warning, "One finding.", ids);

        ids.Clear();

        Assert.Single(result.ObjectIds);
        Assert.Throws<ArgumentException>(() => new RuleResult(" ", "Test", FindingSeverity.Pass, "OK", Array.Empty<Guid>()));
        Assert.Throws<ArgumentException>(() => new RuleResult("MR-1", " ", FindingSeverity.Pass, "OK", Array.Empty<Guid>()));
        Assert.Throws<ArgumentException>(() => new RuleResult("MR-1", "Test", FindingSeverity.Pass, " ", Array.Empty<Guid>()));
        Assert.Throws<ArgumentNullException>(() => new RuleResult("MR-1", "Test", FindingSeverity.Pass, "OK", null!));
        Assert.Throws<ArgumentException>(() => new RuleResult("MR-1", "Test", FindingSeverity.Warning, "Bad ID", new[] { Guid.Empty }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RuleResult("MR-1", "Test", (FindingSeverity)99, "Bad severity", Array.Empty<Guid>()));
    }

    private static ModelObjectSnapshot CreateObject(string? duplicateSetId = null)
    {
        return new ModelObjectSnapshot(
            ObjectId,
            "Brep",
            "Envelope",
            false,
            true,
            0.5,
            25.0,
            duplicateSetId);
    }

    private static StudioSubmissionProfile CreateProfile(
        string name = "Studio Submission",
        string expectedUnits = "Millimeters",
        double minTolerance = 0.000001,
        double maxTolerance = 0.001,
        double farFromOrigin = 1000.0,
        double tinyGeometry = 0.001)
    {
        return new StudioSubmissionProfile(
            name,
            expectedUnits,
            minTolerance,
            maxTolerance,
            farFromOrigin,
            tinyGeometry);
    }
}
