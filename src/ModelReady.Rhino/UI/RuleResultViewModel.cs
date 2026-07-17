using System;
using System.Collections.Generic;
using ModelReady.Core;

namespace ModelReady.Rhino.UI;

public sealed class RuleResultViewModel
{
    public RuleResultViewModel(RuleResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        RuleId = result.RuleId;
        Title = result.Title;
        Severity = result.Severity;
        StatusText = ToStatusText(result.Severity);
        Message = result.Message;
        AffectedObjectCount = result.ObjectIds.Count;
        ObjectIds = result.ObjectIds;
    }

    public string RuleId { get; }

    public string Title { get; }

    public FindingSeverity Severity { get; }

    public string StatusText { get; }

    public string Message { get; }

    public int AffectedObjectCount { get; }

    public IReadOnlyList<Guid> ObjectIds { get; }

    private static string ToStatusText(FindingSeverity severity)
    {
        return severity switch
        {
            FindingSeverity.Pass => "PASS",
            FindingSeverity.Warning => "WARNING",
            FindingSeverity.Fail => "FAIL",
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown finding severity."),
        };
    }
}
