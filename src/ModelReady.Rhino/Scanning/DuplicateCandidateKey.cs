using System;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ModelReady.Rhino.Scanning;

internal readonly struct DuplicateCandidateKey : IEquatable<DuplicateCandidateKey>
{
    private DuplicateCandidateKey(
        ObjectType objectType,
        double minX,
        double minY,
        double minZ,
        double maxX,
        double maxY,
        double maxZ)
    {
        ObjectType = objectType;
        MinX = minX;
        MinY = minY;
        MinZ = minZ;
        MaxX = maxX;
        MaxY = maxY;
        MaxZ = maxZ;
    }

    private ObjectType ObjectType { get; }

    private double MinX { get; }

    private double MinY { get; }

    private double MinZ { get; }

    private double MaxX { get; }

    private double MaxY { get; }

    private double MaxZ { get; }

    public static bool TryCreate(RhinoObject modelObject, double tolerance, out DuplicateCandidateKey key)
    {
        if (modelObject is null)
        {
            throw new ArgumentNullException(nameof(modelObject));
        }

        if (tolerance <= 0 || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "Tolerance must be finite and greater than zero.");
        }

        var boundingBox = modelObject.Geometry.GetBoundingBox(accurate: true);
        if (!boundingBox.IsValid || !IsFinite(boundingBox.Min) || !IsFinite(boundingBox.Max))
        {
            key = default;
            return false;
        }

        key = new DuplicateCandidateKey(
            modelObject.ObjectType,
            Quantize(boundingBox.Min.X, tolerance),
            Quantize(boundingBox.Min.Y, tolerance),
            Quantize(boundingBox.Min.Z, tolerance),
            Quantize(boundingBox.Max.X, tolerance),
            Quantize(boundingBox.Max.Y, tolerance),
            Quantize(boundingBox.Max.Z, tolerance));
        return true;
    }

    public bool Equals(DuplicateCandidateKey other)
    {
        return ObjectType == other.ObjectType
            && MinX.Equals(other.MinX)
            && MinY.Equals(other.MinY)
            && MinZ.Equals(other.MinZ)
            && MaxX.Equals(other.MaxX)
            && MaxY.Equals(other.MaxY)
            && MaxZ.Equals(other.MaxZ);
    }

    public override bool Equals(object? obj)
    {
        return obj is DuplicateCandidateKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(ObjectType);
        hashCode.Add(MinX);
        hashCode.Add(MinY);
        hashCode.Add(MinZ);
        hashCode.Add(MaxX);
        hashCode.Add(MaxY);
        hashCode.Add(MaxZ);
        return hashCode.ToHashCode();
    }

    private static double Quantize(double value, double tolerance)
    {
        return Math.Round(value / tolerance, MidpointRounding.AwayFromZero);
    }

    private static bool IsFinite(Point3d point)
    {
        return IsFinite(point.X) && IsFinite(point.Y) && IsFinite(point.Z);
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
