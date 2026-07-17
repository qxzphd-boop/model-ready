using System;

namespace ModelReady.Core;

public sealed class StudioSubmissionProfile
{
    public StudioSubmissionProfile(
        string name,
        string expectedUnitSystem,
        double minToleranceMetres,
        double maxToleranceMetres,
        double farFromOriginMetres,
        double tinyGeometryMetres)
    {
        Name = Guard.NotBlank(name, nameof(name));
        ExpectedUnitSystem = Guard.NotBlank(expectedUnitSystem, nameof(expectedUnitSystem));
        MinToleranceMetres = Guard.NonNegativeFinite(minToleranceMetres, nameof(minToleranceMetres));
        MaxToleranceMetres = Guard.NonNegativeFinite(maxToleranceMetres, nameof(maxToleranceMetres));
        FarFromOriginMetres = Guard.NonNegativeFinite(farFromOriginMetres, nameof(farFromOriginMetres));
        TinyGeometryMetres = Guard.NonNegativeFinite(tinyGeometryMetres, nameof(tinyGeometryMetres));

        if (MinToleranceMetres > MaxToleranceMetres)
        {
            throw new ArgumentException("Minimum tolerance cannot exceed maximum tolerance.", nameof(minToleranceMetres));
        }
    }

    public string Name { get; }

    public string ExpectedUnitSystem { get; }

    public double MinToleranceMetres { get; }

    public double MaxToleranceMetres { get; }

    public double FarFromOriginMetres { get; }

    public double TinyGeometryMetres { get; }

    public static StudioSubmissionProfile CreateDefault()
    {
        return new StudioSubmissionProfile(
            "Studio Submission",
            "Millimeters",
            0.000001,
            0.001,
            1000.0,
            0.001);
    }
}
