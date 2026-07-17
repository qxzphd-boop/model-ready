using System;

namespace ModelReady.Core;

public sealed class ModelObjectSnapshot
{
    public ModelObjectSnapshot(
        Guid objectId,
        string geometryType,
        string layerName,
        bool isOnDefaultLayer,
        bool isValid,
        double boundingBoxDiagonalMetres,
        double boundingBoxCentreDistanceMetres,
        string? duplicateSetId)
    {
        ObjectId = Guard.NonEmpty(objectId, nameof(objectId));
        GeometryType = Guard.NotBlank(geometryType, nameof(geometryType));
        LayerName = Guard.NotBlank(layerName, nameof(layerName));
        IsOnDefaultLayer = isOnDefaultLayer;
        IsValid = isValid;
        BoundingBoxDiagonalMetres = Guard.NonNegativeFinite(boundingBoxDiagonalMetres, nameof(boundingBoxDiagonalMetres));
        BoundingBoxCentreDistanceMetres = Guard.NonNegativeFinite(boundingBoxCentreDistanceMetres, nameof(boundingBoxCentreDistanceMetres));
        DuplicateSetId = string.IsNullOrWhiteSpace(duplicateSetId) ? null : duplicateSetId!.Trim();
    }

    public Guid ObjectId { get; }

    public string GeometryType { get; }

    public string LayerName { get; }

    public bool IsOnDefaultLayer { get; }

    public bool IsValid { get; }

    public double BoundingBoxDiagonalMetres { get; }

    public double BoundingBoxCentreDistanceMetres { get; }

    public string? DuplicateSetId { get; }
}
