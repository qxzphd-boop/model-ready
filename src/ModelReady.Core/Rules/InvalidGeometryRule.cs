using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public sealed class InvalidGeometryRule : ModelReadyRuleBase
{
    public InvalidGeometryRule()
        : base("MR-GEO-001", "Invalid geometry")
    {
    }

    protected override RuleResult EvaluateCore(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        var affected = new List<Guid>();
        foreach (var modelObject in document.Objects)
        {
            if (!modelObject.IsValid)
            {
                affected.Add(modelObject.ObjectId);
            }
        }

        return affected.Count == 0
            ? Result(FindingSeverity.Pass, "All scanned geometry is valid.", affected)
            : Result(FindingSeverity.Fail, $"Found {affected.Count} invalid object(s).", affected);
    }
}
