using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ModelReady.Core;

public sealed class PreflightRunner
{
    private readonly IReadOnlyList<IModelReadyRule> _rules;

    public PreflightRunner()
        : this(StudioSubmissionRuleSet.Create())
    {
    }

    public PreflightRunner(IEnumerable<IModelReadyRule> rules)
    {
        _rules = Guard.ReadOnlyCopy(rules, nameof(rules), rejectNullItems: true);
        if (_rules.Count == 0)
        {
            throw new ArgumentException("At least one rule is required.", nameof(rules));
        }

        var seenRuleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in _rules)
        {
            Guard.NotBlank(rule.RuleId, nameof(rules));
            if (!seenRuleIds.Add(rule.RuleId))
            {
                throw new ArgumentException($"Duplicate rule ID '{rule.RuleId}'.", nameof(rules));
            }
        }
    }

    public PreflightReport Run(DocumentSnapshot document, StudioSubmissionProfile profile)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var stopwatch = Stopwatch.StartNew();
        var results = new List<RuleResult>(_rules.Count);
        var errors = new List<RuleExecutionError>();

        foreach (var rule in _rules)
        {
            try
            {
                var result = rule.Evaluate(document, profile);
                if (result is null)
                {
                    throw new InvalidOperationException("Rule returned no result.");
                }

                if (!string.Equals(result.RuleId, rule.RuleId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Rule returned ID '{result.RuleId}' instead of '{rule.RuleId}'.");
                }

                results.Add(result);
            }
            catch (Exception exception)
            {
                var exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
                var message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Rule threw an exception without a message."
                    : exception.Message;
                errors.Add(new RuleExecutionError(rule.RuleId, exceptionType, message));
            }
        }

        stopwatch.Stop();
        ReadinessStatus? readiness = errors.Count == 0
            ? ReadinessCalculator.Calculate(results)
            : null;

        return new PreflightReport(document, profile, results, errors, readiness, stopwatch.Elapsed);
    }
}
