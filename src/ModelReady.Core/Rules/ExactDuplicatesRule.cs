using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public sealed class ExactDuplicatesRule : ModelReadyRuleBase
{
    public ExactDuplicatesRule()
        : base("MR-GEO-002", "Exact duplicate geometry")
    {
    }

    protected override RuleResult EvaluateCore(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        var setCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var modelObject in document.Objects)
        {
            if (modelObject.DuplicateSetId is null)
            {
                continue;
            }

            setCounts.TryGetValue(modelObject.DuplicateSetId, out var count);
            setCounts[modelObject.DuplicateSetId] = count + 1;
        }

        var affected = new List<Guid>();
        foreach (var modelObject in document.Objects)
        {
            if (modelObject.DuplicateSetId is not null && setCounts[modelObject.DuplicateSetId] >= 2)
            {
                affected.Add(modelObject.ObjectId);
            }
        }

        return affected.Count == 0
            ? Result(FindingSeverity.Pass, "No exact duplicate sets were supplied by the model adapter.", affected)
            : Result(FindingSeverity.Warning, $"Found {affected.Count} object(s) in exact duplicate sets.", affected);
    }
}
