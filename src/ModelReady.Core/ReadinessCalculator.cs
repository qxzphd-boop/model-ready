using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public static class ReadinessCalculator
{
    public static ReadinessStatus Calculate(IEnumerable<RuleResult> results)
    {
        if (results is null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        var hasResult = false;
        var hasWarning = false;
        var hasFailure = false;

        foreach (var result in results)
        {
            if (result is null)
            {
                throw new ArgumentException("Results cannot contain null values.", nameof(results));
            }

            hasResult = true;
            if (result.Severity == FindingSeverity.Fail)
            {
                hasFailure = true;
            }
            else if (result.Severity == FindingSeverity.Warning)
            {
                hasWarning = true;
            }
        }

        if (!hasResult)
        {
            throw new ArgumentException("At least one rule result is required.", nameof(results));
        }

        if (hasFailure)
        {
            return ReadinessStatus.NotReady;
        }

        return hasWarning ? ReadinessStatus.ReadyWithWarnings : ReadinessStatus.Ready;
    }
}
