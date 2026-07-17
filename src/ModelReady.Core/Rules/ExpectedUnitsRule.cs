using System;

namespace ModelReady.Core;

public sealed class ExpectedUnitsRule : ModelReadyRuleBase
{
    public ExpectedUnitsRule()
        : base("MR-DOC-001", "Expected units")
    {
    }

    protected override RuleResult EvaluateCore(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        var matches = string.Equals(document.UnitSystem, profile.ExpectedUnitSystem, StringComparison.OrdinalIgnoreCase);
        return matches
            ? Result(FindingSeverity.Pass, $"Document units are {document.UnitSystem}.", Array.Empty<Guid>())
            : Result(
                FindingSeverity.Fail,
                $"Document units are {document.UnitSystem}; expected {profile.ExpectedUnitSystem}.",
                Array.Empty<Guid>());
    }
}
