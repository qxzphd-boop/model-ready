using ModelReady.Core;

namespace ModelReady.Core.Tests;

public sealed class ReadinessCalculatorTests
{
    [Fact]
    public void Calculate_rejects_null_and_empty_results()
    {
        Assert.Throws<ArgumentNullException>(() => ReadinessCalculator.Calculate(null!));
        Assert.Throws<ArgumentException>(() => ReadinessCalculator.Calculate(Array.Empty<RuleResult>()));
    }

    [Fact]
    public void Calculate_returns_ready_for_pass_only_results()
    {
        var status = ReadinessCalculator.Calculate(new[] { Result(FindingSeverity.Pass) });

        Assert.Equal(ReadinessStatus.Ready, status);
    }

    [Fact]
    public void Calculate_returns_ready_with_warnings_when_no_rule_fails()
    {
        var status = ReadinessCalculator.Calculate(new[]
        {
            Result(FindingSeverity.Pass),
            Result(FindingSeverity.Warning),
        });

        Assert.Equal(ReadinessStatus.ReadyWithWarnings, status);
    }

    [Fact]
    public void Calculate_gives_failures_precedence_over_warnings()
    {
        var status = ReadinessCalculator.Calculate(new[]
        {
            Result(FindingSeverity.Warning),
            Result(FindingSeverity.Fail),
            Result(FindingSeverity.Pass),
        });

        Assert.Equal(ReadinessStatus.NotReady, status);
    }

    [Fact]
    public void Calculate_rejects_a_sequence_containing_null()
    {
        Assert.Throws<ArgumentException>(() => ReadinessCalculator.Calculate(new RuleResult[] { Result(FindingSeverity.Pass), null! }));
        Assert.Throws<ArgumentException>(() => ReadinessCalculator.Calculate(new RuleResult[] { Result(FindingSeverity.Fail), null! }));
    }

    private static RuleResult Result(FindingSeverity severity)
    {
        return new RuleResult("MR-TEST-001", "Test rule", severity, "Test result.", Array.Empty<Guid>());
    }
}
