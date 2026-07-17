using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public sealed class FarFromOriginRule : ModelReadyRuleBase
{
    public FarFromOriginRule()
        : base("MR-GEO-003", "Far from origin")
    {
    }

    protected override RuleResult EvaluateCore(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        var affected = new List<Guid>();
        foreach (var modelObject in document.Objects)
        {
            if (modelObject.BoundingBoxCentreDistanceMetres > profile.FarFromOriginMetres)
            {
                affected.Add(modelObject.ObjectId);
            }
        }

        return affected.Count == 0
            ? Result(FindingSeverity.Pass, $"All objects are within {profile.FarFromOriginMetres:G} m of the origin.", affected)
            : Result(FindingSeverity.Warning, $"Found {affected.Count} object(s) beyond {profile.FarFromOriginMetres:G} m.", affected);
    }
}
