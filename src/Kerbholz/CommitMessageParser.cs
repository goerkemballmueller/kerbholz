using System.Text.RegularExpressions;

namespace Kerbholz;

/// <summary>
/// Splits a raw commit message into a <see cref="CommitMessage"/>. It reads the message the way git keeps it: comment lines and everything
/// below the scissors line of <c>git commit --verbose</c> are dropped, because a <c>commit-msg</c> hook receives the file before git cleans it.
/// </summary>
public static partial class CommitMessageParser
{
    /// <summary>The character git starts comment lines with unless <c>core.commentChar</c> says otherwise.</summary>
    public const char DefaultCommentChar = '#';

    /// <summary>Parses <paramref name="raw"/>; never throws for any text, an empty message gives an empty header.</summary>
    /// <param name="raw">The message as git wrote it into the file it hands to the hook.</param>
    /// <param name="commentChar">The comment character of the repository (<c>core.commentChar</c>).</param>
    public static CommitMessage Parse(string raw, char commentChar = DefaultCommentChar)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var lines = KeptLines(raw, commentChar);
        var header = lines.Count > 0 ? lines[0] : string.Empty;
        var secondLine = lines.Count > 1 ? lines[1] : null;

        var rest = lines.Skip(1).SkipWhile(string.IsNullOrEmpty).ToList();
        var footerStart = FooterStart(rest);
        var body = string.Join('\n', rest.Take(footerStart)).TrimEnd('\n');
        var footers = Footers(rest.Skip(footerStart));

        var match = HeaderPattern().Match(header);
        return match.Success
            ? new CommitMessage(
                header,
                match.Groups["type"].Value,
                match.Groups["scope"].Success ? match.Groups["scope"].Value : null,
                match.Groups["bang"].Success,
                match.Groups["description"].Value,
                secondLine,
                body,
                footers)
            : new CommitMessage(header, null, null, false, null, secondLine, body, footers);
    }

    // type, optional (scope), optional !, then ": ". The scope may be empty and the description missing here so that a rule can name
    // those mistakes; "feat: " arrives as "feat:" because trailing spaces are trimmed, and must read as an empty description, not as
    // a header of the wrong form. "feat:x" (no space) does not match.
    [GeneratedRegex(@"^(?<type>[A-Za-z][A-Za-z0-9-]*)(?:\((?<scope>[^()]*)\))?(?<bang>!)?:(?: (?<description>.*))?$", RegexOptions.CultureInvariant)]
    private static partial Regex HeaderPattern();

    // A footer starts with a token and ": " or " #" (git's trailer convention). "#" stays part of the value, so "Closes #42" keeps "#42".
    // The breaking-change token is accepted in any case here so that a rule can point out the lower-case form, which does not count.
    [GeneratedRegex(@"^(?<token>(?i:BREAKING[ -]CHANGE)|[A-Za-z][A-Za-z0-9-]*)(?:: | (?=#))(?<value>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex FooterPattern();

    /// <summary>Whether <paramref name="line"/> starts a footer.</summary>
    public static bool IsFooterLine(string line) => FooterPattern().IsMatch(line);

    // The lines git would keep: line ends made "\n", comment lines dropped, nothing below the scissors line, trailing spaces and the blank
    // lines at both ends removed.
    private static List<string> KeptLines(string raw, char commentChar)
    {
        var scissors = commentChar + " ------------------------ >8 ------------------------";
        var kept = new List<string>();
        foreach (var line in raw.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            if (line.StartsWith(scissors, StringComparison.Ordinal))
                break;
            if (line.StartsWith(commentChar))
                continue;
            kept.Add(line.TrimEnd());
        }

        while (kept.Count > 0 && kept[0].Length == 0)
            kept.RemoveAt(0);
        while (kept.Count > 0 && kept[^1].Length == 0)
            kept.RemoveAt(kept.Count - 1);
        return kept;
    }

    // The footers are the last paragraph, and only when its first line is a footer line; a body paragraph that merely contains "Note: …"
    // further down stays body. Returns the index in rest where the footers begin, or rest.Count when there are none.
    private static int FooterStart(List<string> rest)
    {
        var start = rest.FindLastIndex(string.IsNullOrEmpty) + 1;
        return start < rest.Count && IsFooterLine(rest[start]) ? start : rest.Count;
    }

    // A line that does not start a footer continues the value of the footer before it (the specification allows multi-line values).
    private static List<Footer> Footers(IEnumerable<string> lines)
    {
        var footers = new List<Footer>();
        foreach (var line in lines)
        {
            var match = FooterPattern().Match(line);
            if (match.Success)
                footers.Add(new Footer(match.Groups["token"].Value, match.Groups["value"].Value));
            else if (footers.Count > 0)
                footers[^1] = footers[^1] with { Value = footers[^1].Value + "\n" + line };
        }

        return footers;
    }
}
