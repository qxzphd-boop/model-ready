using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public sealed class PreflightReport
{
    public PreflightReport(
        DocumentSnapshot document,
        StudioSubmissionProfile profile,
        IEnumerable<RuleResult> results,
        IEnumerable<RuleExecutionError> errors,
        ReadinessStatus? readiness,
        TimeSpan elapsed)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Results = Guard.ReadOnlyCopy(results, nameof(results), rejectNullItems: true);
        Errors = Guard.ReadOnlyCopy(errors, nameof(errors), rejectNullItems: true);

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Elapsed time cannot be negative.");
        }

        if (readiness.HasValue && !Enum.IsDefined(typeof(ReadinessStatus), readiness.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(readiness), readiness, "Unknown readiness status.");
        }

        if (Errors.Count == 0 && !readiness.HasValue)
        {
            throw new ArgumentException("A successful scan must include a readiness status.", nameof(readiness));
        }

        if (Errors.Count > 0 && readiness.HasValue)
        {
            throw new ArgumentException("A scan containing rule errors cannot report readiness.", nameof(readiness));
        }

        if (Results.Count == 0 && Errors.Count == 0)
        {
            throw new ArgumentException("A report must contain at least one result or error.", nameof(results));
        }

        if (Errors.Count == 0 && readiness.HasValue)
        {
            var calculatedReadiness = ReadinessCalculator.Calculate(Results);
            if (calculatedReadiness != readiness.Value)
            {
                throw new ArgumentException(
                    $"Readiness '{readiness.Value}' does not match calculated readiness '{calculatedReadiness}'.",
                    nameof(readiness));
            }
        }

        Readiness = readiness;
        Elapsed = elapsed;
    }

    public DocumentSnapshot Document { get; }

    public StudioSubmissionProfile Profile { get; }

    public IReadOnlyList<RuleResult> Results { get; }

    public IReadOnlyList<RuleExecutionError> Errors { get; }

    public ReadinessStatus? Readiness { get; }

    public TimeSpan Elapsed { get; }
}
