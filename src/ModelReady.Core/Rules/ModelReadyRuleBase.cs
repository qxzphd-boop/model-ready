using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public abstract class ModelReadyRuleBase : IModelReadyRule
{
    protected ModelReadyRuleBase(string ruleId, string title)
    {
        RuleId = Guard.NotBlank(ruleId, nameof(ruleId));
        Title = Guard.NotBlank(title, nameof(title));
    }

    public string RuleId { get; }

    protected string Title { get; }

    public RuleResult Evaluate(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        return EvaluateCore(document, profile);
    }

    protected RuleResult Result(FindingSeverity severity, string message, IEnumerable<Guid> objectIds)
    {
        return new RuleResult(RuleId, Title, severity, message, objectIds);
    }

    protected abstract RuleResult EvaluateCore(DocumentSnapshot document, StudioSubmissionProfile profile);
}
