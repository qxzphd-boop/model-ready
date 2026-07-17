using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public sealed class DefaultLayerRule : ModelReadyRuleBase
{
    public DefaultLayerRule()
        : base("MR-LAY-001", "Geometry on Default layer")
    {
    }

    protected override RuleResult EvaluateCore(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        var affected = new List<Guid>();
        foreach (var modelObject in document.Objects)
        {
            if (modelObject.IsOnDefaultLayer)
            {
                affected.Add(modelObject.ObjectId);
            }
        }

        return affected.Count == 0
            ? Result(FindingSeverity.Pass, "No scanned geometry is on the Default layer.", affected)
            : Result(FindingSeverity.Warning, $"Found {affected.Count} object(s) on the Default layer.", affected);
    }
}
