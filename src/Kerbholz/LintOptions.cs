namespace Kerbholz;

/// <summary>The settings of a check. The defaults follow the Conventional Commits specification and the types most projects use.</summary>
public sealed record LintOptions
{
    /// <summary>The types the specification names (<c>feat</c>, <c>fix</c>) and the ones the Angular convention added, which most tools accept.</summary>
    public static readonly IReadOnlyList<string> DefaultTypes =
        ["feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "revert"];

    /// <summary>The types a header may use; compared without regard to case, as the specification asks.</summary>
    public IReadOnlyList<string> Types { get; init; } = DefaultTypes;

    /// <summary>The longest header in characters as a reader counts them (an emoji or a letter with an accent is one).</summary>
    public int MaxHeaderLength { get; init; } = 100;

    /// <summary>Whether messages git writes itself (merges, reverts) and autosquash commits (<c>fixup!</c>, <c>squash!</c>, <c>amend!</c>) pass unchecked.</summary>
    public bool SkipGitGenerated { get; init; } = true;

    /// <summary>The comment character of the repository (<c>core.commentChar</c>).</summary>
    public char CommentChar { get; init; } = CommitMessageParser.DefaultCommentChar;
}
