namespace Kerbholz;

/// <summary>One footer of a commit message, such as <c>Refs: #12</c> or <c>BREAKING CHANGE: the config file moved</c>.</summary>
/// <param name="Token">The footer's token as written (<c>Refs</c>, <c>Reviewed-by</c>, <c>BREAKING CHANGE</c>).</param>
/// <param name="Value">The footer's value; it may span several lines.</param>
public sealed record Footer(string Token, string Value)
{
    /// <summary>
    /// Whether this footer announces a breaking change. The specification makes this the one case-sensitive part of a message:
    /// only <c>BREAKING CHANGE</c> or its synonym <c>BREAKING-CHANGE</c>, written in capitals, counts.
    /// </summary>
    public bool IsBreakingChange => Token is "BREAKING CHANGE" or "BREAKING-CHANGE";
}

/// <summary>
/// A commit message split into the parts of the Conventional Commits specification. The parser is lenient: a header that does not follow
/// the <c>type(scope)!: description</c> form still gives a message, with <see cref="Type"/> left null, so the rules can say what is wrong
/// instead of the parser throwing.
/// </summary>
/// <param name="Header">The first line, exactly as written.</param>
/// <param name="Type">The type (<c>feat</c>, <c>fix</c>, …), or null when the header does not have the required form.</param>
/// <param name="Scope">The text between the parentheses, or null when there are none. An empty pair of parentheses gives an empty string.</param>
/// <param name="HasBreakingMarker">Whether the header carries the <c>!</c> before the colon.</param>
/// <param name="Description">The text after <c>": "</c>, or null when the header does not have the required form.</param>
/// <param name="SecondLine">The line after the header, or null for a one-line message; the specification wants it blank.</param>
/// <param name="Body">The free text between the header and the footers, without the blank lines around it; empty when there is none.</param>
/// <param name="Footers">The footers in the order they were written.</param>
public sealed record CommitMessage(
    string Header,
    string? Type,
    string? Scope,
    bool HasBreakingMarker,
    string? Description,
    string? SecondLine,
    string Body,
    IReadOnlyList<Footer> Footers)
{
    /// <summary>Whether the header has the <c>type(scope)!: description</c> form.</summary>
    public bool HasConventionalHeader => Type is not null;

    /// <summary>Whether the commit announces a breaking change, by the <c>!</c> in the header or by a breaking-change footer.</summary>
    public bool IsBreaking => HasBreakingMarker || Footers.Any(f => f.IsBreakingChange);
}
