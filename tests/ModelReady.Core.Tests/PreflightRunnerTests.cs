using ModelReady.Core;

namespace ModelReady.Core.Tests;

public sealed class PreflightRunnerTests
{
    [Fact]
    public void Default_runner_returns_all_seven_results_in_frozen_order()
    {
        var report = new PreflightRunner().Run(RuleTestData.Document(), RuleTestData.Profile());

        Assert.Equal(new[]
        {
            "MR-DOC-001",
            "MR-DOC-002",
            "MR-GEO-001",
            "MR-GEO-002",
            "MR-GEO-003",
            "MR-GEO-004",
            "MR-LAY-001",
        }, report.Results.Select(result => result.RuleId));
        Assert.Empty(report.Errors);
        Assert.Equal(ReadinessStatus.Ready, report.Readiness);
        Assert.True(report.Elapsed >= TimeSpan.Zero);
    }

    [Fact]
    public void Default_runner_calculates_not_ready_when_a_rule_fails()
    {
        var report = new PreflightRunner().Run(
            RuleTestData.Document(RuleTestData.Object(isValid: false)),
            RuleTestData.Profile());

        Assert.Equal(ReadinessStatus.NotReady, report.Readiness);
    }

    [Fact]
    public void Runner_continues_after_a_rule_throws_and_suppresses_readiness()
    {
        var runner = new PreflightRunner(new IModelReadyRule[]
        {
            new StubRule("MR-TEST-001", FindingSeverity.Pass),
            new ThrowingRule("MR-TEST-002"),
            new StubRule("MR-TEST-003", FindingSeverity.Warning),
        });

        var report = runner.Run(RuleTestData.Document(), RuleTestData.Profile());

        Assert.Equal(new[] { "MR-TEST-001", "MR-TEST-003" }, report.Results.Select(result => result.RuleId));
        var error = Assert.Single(report.Errors);
        Assert.Equal("MR-TEST-002", error.RuleId);
        Assert.Equal(typeof(InvalidOperationException).FullName, error.ExceptionType);
        Assert.Equal("Synthetic rule failure.", error.Message);
        Assert.Null(report.Readiness);
    }

    [Fact]
    public void Runner_rejects_null_or_empty_inputs()
    {
        Assert.Throws<ArgumentNullException>(() => new PreflightRunner(null!));
        Assert.Throws<ArgumentException>(() => new PreflightRunner(Array.Empty<IModelReadyRule>()));
        Assert.Throws<ArgumentException>(() => new PreflightRunner(new IModelReadyRule[] { null! }));

        var runner = new PreflightRunner();
        Assert.Throws<ArgumentNullException>(() => runner.Run(null!, RuleTestData.Profile()));
        Assert.Throws<ArgumentNullException>(() => runner.Run(RuleTestData.Document(), null!));
    }

    [Fact]
    public void Report_copies_results_and_errors_and_enforces_readiness_consistency()
    {
        var results = new List<RuleResult>
        {
            new("MR-TEST-001", "Test", FindingSeverity.Pass, "Passed.", Array.Empty<Guid>()),
        };
        var errors = new List<RuleExecutionError>();
        var report = new PreflightReport(
            RuleTestData.Document(),
            RuleTestData.Profile(),
            results,
            errors,
            ReadinessStatus.Ready,
            TimeSpan.FromMilliseconds(1));

        results.Clear();
        errors.Add(new RuleExecutionError("MR-TEST-002", "System.Exception", "Late mutation."));

        Assert.Single(report.Results);
        Assert.Empty(report.Errors);
        Assert.Throws<ArgumentException>(() => new PreflightReport(
            RuleTestData.Document(),
            RuleTestData.Profile(),
            report.Results,
            Array.Empty<RuleExecutionError>(),
            null,
            TimeSpan.Zero));
        Assert.Throws<ArgumentException>(() => new PreflightReport(
            RuleTestData.Document(),
            RuleTestData.Profile(),
            report.Results,
            new[] { new RuleExecutionError("MR-TEST-002", "System.Exception", "Failed.") },
            ReadinessStatus.Ready,
            TimeSpan.Zero));
    }

    [Fact]
    public void Error_and_report_models_reject_invalid_values()
    {
        Assert.Throws<ArgumentException>(() => new RuleExecutionError(" ", "System.Exception", "Failed."));
        Assert.Throws<ArgumentException>(() => new RuleExecutionError("MR-1", " ", "Failed."));
        Assert.Throws<ArgumentException>(() => new RuleExecutionError("MR-1", "System.Exception", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreflightReport(
            RuleTestData.Document(),
            RuleTestData.Profile(),
            new[] { new RuleResult("MR-1", "Test", FindingSeverity.Pass, "Passed.", Array.Empty<Guid>()) },
            Array.Empty<RuleExecutionError>(),
            ReadinessStatus.Ready,
            TimeSpan.FromMilliseconds(-1)));
    }

    private sealed class StubRule : IModelReadyRule
    {
        private readonly FindingSeverity _severity;

        public StubRule(string ruleId, FindingSeverity severity)
        {
            RuleId = ruleId;
            _severity = severity;
        }

        public string RuleId { get; }

        public RuleResult Evaluate(DocumentSnapshot document, StudioSubmissionProfile profile)
        {
            return new RuleResult(RuleId, "Stub rule", _severity, "Stub result.", Array.Empty<Guid>());
        }
    }

    private sealed class ThrowingRule : IModelReadyRule
    {
        public ThrowingRule(string ruleId)
        {
            RuleId = ruleId;
        }

        public string RuleId { get; }

        public RuleResult Evaluate(DocumentSnapshot document, StudioSubmissionProfile profile)
        {
            throw new InvalidOperationException("Synthetic rule failure.");
        }
    }
}
