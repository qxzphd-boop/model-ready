using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public sealed class TinyGeometryRule : ModelReadyRuleBase
{
    public TinyGeometryRule()
        : base("MR-GEO-004", "Tiny geometry")
    {
    }

    protected override RuleResult EvaluateCore(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        var affected = new List<Guid>();
        foreach (var modelObject in document.Objects)
        {
            if (modelObject.BoundingBoxDiagonalMetres < profile.TinyGeometryMetres)
            {
                affected.Add(modelObject.ObjectId);
            }
        }

        return affected.Count == 0
            ? Result(FindingSeverity.Pass, $"All object diagonals are at least {profile.TinyGeometryMetres:G} m.", affected)
            : Result(FindingSeverity.Warning, $"Found {affected.Count} object(s) below {profile.TinyGeometryMetres:G} m.", affected);
    }
}
