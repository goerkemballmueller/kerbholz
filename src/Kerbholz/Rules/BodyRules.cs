namespace Kerbholz.Rules;

/// <summary><c>body-leading-blank</c>: a blank line must separate the header from what follows, or git and every tool that reads the
/// first paragraph as the title will take the second line as part of it.</summary>
public sealed class BodyLeadingBlankRule : ICommitRule
{
    /// <inheritdoc/>
    public string Id => "body-leading-blank";

    /// <inheritdoc/>
    public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!string.IsNullOrEmpty(message.SecondLine))
            yield return new Finding(Id, Severity.Error, "Leave the second line blank; the body starts on the third.");
    }
}

/// <summary><c>breaking-change-capitals</c>: a breaking-change footer only counts in capitals, so a lower-case one silently announces nothing.</summary>
public sealed class BreakingChangeCapitalsRule : ICommitRule
{
    /// <inheritdoc/>
    public string Id => "breaking-change-capitals";

    /// <inheritdoc/>
    public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        foreach (var footer in message.Footers)
        {
            var looksBreaking = footer.Token.Equals("BREAKING CHANGE", StringComparison.OrdinalIgnoreCase)
                || footer.Token.Equals("BREAKING-CHANGE", StringComparison.OrdinalIgnoreCase);
            if (looksBreaking && !footer.IsBreakingChange)
                yield return new Finding(Id, Severity.Error, $"Write \"{footer.Token}\" as \"BREAKING CHANGE\"; in any other case it does not mark a breaking change.");
        }
    }
}
