namespace ModelReady.Core;

public interface IModelReadyRule
{
    string RuleId { get; }

    RuleResult Evaluate(DocumentSnapshot document, StudioSubmissionProfile profile);
}
