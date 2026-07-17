using System;
using System.Collections.Generic;
using ModelReady.Core;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ModelReady.Rhino.Scanning;

public sealed class RhinoDocumentSnapshotFactory : IRhinoDocumentSnapshotFactory
{
    private const ObjectType SupportedObjectTypes = ObjectType.Curve
        | ObjectType.Brep
        | ObjectType.Extrusion
        | ObjectType.Mesh
        | ObjectType.SubD;

    private readonly RhinoDuplicateDetector _duplicateDetector;

    public RhinoDocumentSnapshotFactory()
        : this(new RhinoDuplicateDetector())
    {
    }

    internal RhinoDocumentSnapshotFactory(RhinoDuplicateDetector duplicateDetector)
    {
        _duplicateDetector = duplicateDetector ?? throw new ArgumentNullException(nameof(duplicateDetector));
    }

    public DocumentSnapshot Create(RhinoDoc document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var modelObjects = GetSupportedObjects(document);
        var duplicateSets = _duplicateDetector.FindDuplicateSets(modelObjects, document.ModelAbsoluteTolerance);
        var metresPerModelUnit = RhinoMath.UnitScale(document.ModelUnitSystem, UnitSystem.Meters);
        var snapshots = new List<ModelObjectSnapshot>(modelObjects.Count);

        foreach (var modelObject in modelObjects)
        {
            var boundingBox = modelObject.Geometry.GetBoundingBox(accurate: true);
            var diagonalMetres = 0.0;
            var centreDistanceMetres = 0.0;
            if (boundingBox.IsValid && IsFinite(boundingBox.Min) && IsFinite(boundingBox.Max))
            {
                diagonalMetres = boundingBox.Diagonal.Length * metresPerModelUnit;
                centreDistanceMetres = boundingBox.Center.DistanceTo(Point3d.Origin) * metresPerModelUnit;
            }

            duplicateSets.TryGetValue(modelObject.Id, out var duplicateSetId);
            snapshots.Add(new ModelObjectSnapshot(
                modelObject.Id,
                modelObject.ObjectType.ToString(),
                GetLayerName(document, modelObject.Attributes.LayerIndex),
                modelObject.Attributes.LayerIndex == 0,
                modelObject.Geometry.IsValid,
                diagonalMetres,
                centreDistanceMetres,
                duplicateSetId));
        }

        var documentName = string.IsNullOrWhiteSpace(document.Name) ? "Untitled" : document.Name;
        return new DocumentSnapshot(
            documentName,
            document.ModelUnitSystem.ToString(),
            document.ModelAbsoluteTolerance * metresPerModelUnit,
            snapshots);
    }

    private static IReadOnlyList<RhinoObject> GetSupportedObjects(RhinoDoc document)
    {
        var settings = new ObjectEnumeratorSettings
        {
            NormalObjects = true,
            LockedObjects = true,
            HiddenObjects = true,
            IdefObjects = false,
            DeletedObjects = false,
            ReferenceObjects = false,
            IncludeLights = false,
            IncludeGrips = false,
            IncludePhantoms = false,
            ObjectTypeFilter = SupportedObjectTypes,
        };

        var supported = new List<RhinoObject>();
        foreach (var modelObject in document.Objects.GetObjectList(settings))
        {
            if (modelObject.IsDeleted
                || modelObject.IsReference
                || modelObject.IsInstanceDefinitionGeometry
                || modelObject.Attributes.Space != ActiveSpace.ModelSpace
                || (modelObject.ObjectType & SupportedObjectTypes) == 0)
            {
                continue;
            }

            supported.Add(modelObject);
        }

        return supported;
    }

    private static string GetLayerName(RhinoDoc document, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= document.Layers.Count)
        {
            return "Unassigned";
        }

        var layerName = document.Layers[layerIndex].Name;
        return string.IsNullOrWhiteSpace(layerName) ? "Unassigned" : layerName;
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
