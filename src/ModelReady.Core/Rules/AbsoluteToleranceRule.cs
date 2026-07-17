using System;

namespace ModelReady.Core;

public sealed class AbsoluteToleranceRule : ModelReadyRuleBase
{
    public AbsoluteToleranceRule()
        : base("MR-DOC-002", "Absolute tolerance")
    {
    }

    protected override RuleResult EvaluateCore(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        var tolerance = document.AbsoluteToleranceMetres;
        var isWithinRange = tolerance >= profile.MinToleranceMetres && tolerance <= profile.MaxToleranceMetres;
        return isWithinRange
            ? Result(FindingSeverity.Pass, $"Absolute tolerance is {tolerance:G} m.", Array.Empty<Guid>())
            : Result(
                FindingSeverity.Warning,
                $"Absolute tolerance {tolerance:G} m is outside {profile.MinToleranceMetres:G}–{profile.MaxToleranceMetres:G} m.",
                Array.Empty<Guid>());
    }
}
