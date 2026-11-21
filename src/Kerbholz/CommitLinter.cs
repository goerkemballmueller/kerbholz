using Kerbholz.Rules;

namespace Kerbholz;

/// <summary>Checks commit messages with a set of rules. The rules come in from outside, so a project adds its own or leaves one out
/// without changing this class.</summary>
public sealed class CommitLinter
{
    // What git writes itself, and what autosquash folds into another commit before it lands: checking these would only teach people
    // to pass --no-verify.
    private static readonly (string Prefix, string Reason)[] GitGenerated =
    [
        ("Merge ", "a merge commit git wrote"),
        ("Revert \"", "a revert git wrote"),
        ("fixup! ", "a fixup commit autosquash folds into another"),
        ("squash! ", "a squash commit autosquash folds into another"),
        ("amend! ", "an amend commit autosquash folds into another"),
    ];

    private readonly IReadOnlyList<ICommitRule> _rules;
    private readonly LintOptions _options;

    /// <summary>A linter with the given rules and settings.</summary>
    public CommitLinter(IEnumerable<ICommitRule> rules, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(options);
        _rules = [.. rules];
        _options = options;
        var duplicate = _rules.GroupBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"The rule \"{duplicate.Key}\" is given twice.", nameof(rules));
    }

    /// <summary>A linter with <see cref="DefaultRules"/> and default settings.</summary>
    public CommitLinter() : this(DefaultRules.Create(), new LintOptions())
    {
    }

    /// <summary>Parses <paramref name="raw"/> and runs every rule on it, unless it is a message git generated (see
    /// <see cref="LintOptions.SkipGitGenerated"/>).</summary>
    public LintReport Lint(string raw)
    {
        var message = CommitMessageParser.Parse(raw, _options.CommentChar);
        if (_options.SkipGitGenerated && GitGenerated.FirstOrDefault(g => message.Header.StartsWith(g.Prefix, StringComparison.Ordinal)) is { Reason: { } reason })
            return new LintReport([], reason);

        return new LintReport([.. _rules.SelectMany(rule => rule.Check(message, _options))]);
    }
}
