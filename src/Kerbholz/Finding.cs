namespace Kerbholz;

/// <summary>How much a finding weighs: an error fails the check, a warning only fails it when warnings count as errors.</summary>
public enum Severity
{
    /// <summary>Worth fixing, does not fail the check by default.</summary>
    Warning,

    /// <summary>Breaks the specification or the repository's settings; fails the check.</summary>
    Error,
}

/// <summary>One thing a rule found in a commit message.</summary>
/// <param name="RuleId">The rule's stable id, such as <c>type-allowed</c>.</param>
/// <param name="Severity">How much it weighs.</param>
/// <param name="Message">What is wrong and how to fix it, in one sentence.</param>
public sealed record Finding(string RuleId, Severity Severity, string Message)
{
    /// <inheritdoc/>
    public override string ToString() => $"{(Severity == Severity.Error ? "error" : "warning")} {RuleId}: {Message}";
}

/// <summary>The result of checking one message: the findings, or why the message was not checked at all.</summary>
/// <param name="Findings">What the rules found, in the order of the rules.</param>
/// <param name="SkippedBecause">Why the rules did not run (a merge or a fixup commit), or null when they ran.</param>
public sealed record LintReport(IReadOnlyList<Finding> Findings, string? SkippedBecause = null)
{
    /// <summary>Whether any finding is an error.</summary>
    public bool HasErrors => Findings.Any(f => f.Severity == Severity.Error);

    /// <summary>Whether the message passes; with <paramref name="warningsAsErrors"/> a warning fails it as well.</summary>
    public bool Passes(bool warningsAsErrors = false) => warningsAsErrors ? Findings.Count == 0 : !HasErrors;
}
