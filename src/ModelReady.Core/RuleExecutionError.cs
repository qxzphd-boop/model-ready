namespace ModelReady.Core;

public sealed class RuleExecutionError
{
    public RuleExecutionError(string ruleId, string exceptionType, string message)
    {
        RuleId = Guard.NotBlank(ruleId, nameof(ruleId));
        ExceptionType = Guard.NotBlank(exceptionType, nameof(exceptionType));
        Message = Guard.NotBlank(message, nameof(message));
    }

    public string RuleId { get; }

    public string ExceptionType { get; }

    public string Message { get; }
}
