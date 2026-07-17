using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public sealed class RuleResult
{
    public RuleResult(
        string ruleId,
        string title,
        FindingSeverity severity,
        string message,
        IEnumerable<Guid> objectIds)
    {
        RuleId = Guard.NotBlank(ruleId, nameof(ruleId));
        Title = Guard.NotBlank(title, nameof(title));
        Message = Guard.NotBlank(message, nameof(message));

        if (!Enum.IsDefined(typeof(FindingSeverity), severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown finding severity.");
        }

        Severity = severity;
        ObjectIds = Guard.ReadOnlyCopy(objectIds, nameof(objectIds), rejectNullItems: false);
        foreach (var objectId in ObjectIds)
        {
            Guard.NonEmpty(objectId, nameof(objectIds));
        }
    }

    public string RuleId { get; }

    public string Title { get; }

    public FindingSeverity Severity { get; }

    public string Message { get; }

    public IReadOnlyList<Guid> ObjectIds { get; }
}
