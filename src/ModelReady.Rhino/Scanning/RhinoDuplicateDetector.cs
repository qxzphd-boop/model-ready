using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ModelReady.Rhino.Scanning;

public sealed class RhinoDuplicateDetector
{
    public IReadOnlyDictionary<Guid, string> FindDuplicateSets(
        IReadOnlyList<RhinoObject> objects,
        double tolerance)
    {
        if (objects is null)
        {
            throw new ArgumentNullException(nameof(objects));
        }

        if (tolerance <= 0 || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "Tolerance must be finite and greater than zero.");
        }

        var buckets = new Dictionary<DuplicateCandidateKey, List<RhinoObject>>();
        var bucketOrder = new List<DuplicateCandidateKey>();

        foreach (var modelObject in objects)
        {
            if (modelObject is null)
            {
                throw new ArgumentException("Object collection cannot contain null values.", nameof(objects));
            }

            if (!DuplicateCandidateKey.TryCreate(modelObject, tolerance, out var key))
            {
                continue;
            }

            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new List<RhinoObject>();
                buckets.Add(key, bucket);
                bucketOrder.Add(key);
            }

            bucket.Add(modelObject);
        }

        var duplicateSets = new Dictionary<Guid, string>();
        var duplicateSetNumber = 0;

        foreach (var key in bucketOrder)
        {
            var equivalentGroups = GroupEquivalentGeometry(buckets[key]);
            foreach (var group in equivalentGroups)
            {
                if (group.Count < 2)
                {
                    continue;
                }

                duplicateSetNumber++;
                var duplicateSetId = $"duplicate-set-{duplicateSetNumber:D4}";
                foreach (var modelObject in group)
                {
                    duplicateSets.Add(modelObject.Id, duplicateSetId);
                }
            }
        }

        return new ReadOnlyDictionary<Guid, string>(duplicateSets);
    }

    private static IReadOnlyList<IReadOnlyList<RhinoObject>> GroupEquivalentGeometry(
        IReadOnlyList<RhinoObject> candidates)
    {
        var groups = new List<List<RhinoObject>>();

        foreach (var candidate in candidates)
        {
            List<RhinoObject>? matchingGroup = null;
            foreach (var group in groups)
            {
                if (GeometryBase.GeometryEquals(group[0].Geometry, candidate.Geometry))
                {
                    matchingGroup = group;
                    break;
                }
            }

            if (matchingGroup is null)
            {
                matchingGroup = new List<RhinoObject>();
                groups.Add(matchingGroup);
            }

            matchingGroup.Add(candidate);
        }

        return groups;
    }
}
