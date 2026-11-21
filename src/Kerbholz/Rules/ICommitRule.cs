namespace Kerbholz.Rules;

/// <summary>One check of a parsed commit message. A rule says only what it is about; a rule that needs the conventional header stays
/// silent when the header has another form, because <see cref="HeaderFormatRule"/> already reports that once.</summary>
public interface ICommitRule
{
    /// <summary>The rule's stable id, used in the output and to switch it off.</summary>
    string Id { get; }

    /// <summary>The findings for <paramref name="message"/>; none when it is fine.</summary>
    IEnumerable<Finding> Check(CommitMessage message, LintOptions options);
}

/// <summary>The rules a check runs when nothing else is configured, in the order their findings are shown.</summary>
public static class DefaultRules
{
    /// <summary>A new list of the default rules.</summary>
    public static IReadOnlyList<ICommitRule> Create() =>
    [
        new HeaderFormatRule(),
        new TypeAllowedRule(),
        new ScopeNotEmptyRule(),
        new DescriptionNotEmptyRule(),
        new HeaderMaxLengthRule(),
        new DescriptionTrailingPeriodRule(),
        new BodyLeadingBlankRule(),
        new BreakingChangeCapitalsRule(),
    ];
}
