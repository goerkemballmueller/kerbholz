using System.Globalization;

namespace Kerbholz.Cli;

/// <summary>What the command line asked for.</summary>
/// <param name="Path">The message file, or null to read standard input.</param>
/// <param name="WarningsAsErrors">Whether a warning fails the check as well.</param>
/// <param name="Options">The settings of the check.</param>
/// <param name="Help">Whether only the usage was asked for.</param>
public sealed record CliArguments(string? Path, bool WarningsAsErrors, LintOptions Options, bool Help)
{
    /// <summary>The usage text.</summary>
    public const string Usage = """
        Usage: kerbholz [<file> | -] [options]

          <file>                    the message file (a commit-msg hook passes it as "$1"); "-" or nothing reads standard input
          --types <a,b,c>           the allowed types instead of the defaults
          --max-header-length <n>   the longest header (default 100)
          --comment-char <c>        the repository's core.commentChar (default #)
          --warnings-as-errors      fail on warnings too
          --no-skip                 also check merges, reverts and fixup!/squash!/amend! commits
          -h, --help                show this text

        Exit codes: 0 passed, 1 the message has errors, 2 wrong arguments or the file cannot be read.
        """;

    /// <summary>Reads <paramref name="args"/>; false with the reason when they do not make sense.</summary>
    public static bool TryParse(IReadOnlyList<string> args, out CliArguments parsed, out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);
        string? path = null;
        var pathGiven = false;
        var warningsAsErrors = false;
        var options = new LintOptions();
        parsed = new CliArguments(null, false, options, false);
        error = null;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    parsed = parsed with { Help = true };
                    return true;
                case "--warnings-as-errors":
                    warningsAsErrors = true;
                    break;
                case "--no-skip":
                    options = options with { SkipGitGenerated = false };
                    break;
                case "--types":
                    if (!TryValue(args, ref i, out var types, out error))
                        return false;
                    var list = types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (list.Length == 0)
                        return Fail("--types needs at least one type.", out error);
                    options = options with { Types = list };
                    break;
                case "--max-header-length":
                    if (!TryValue(args, ref i, out var number, out error))
                        return false;
                    if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var max) || max < 1)
                        return Fail($"--max-header-length needs a whole number above 0, not \"{number}\".", out error);
                    options = options with { MaxHeaderLength = max };
                    break;
                case "--comment-char":
                    if (!TryValue(args, ref i, out var commentChar, out error))
                        return false;
                    // git's "auto" picks a character the message does not start a line with; we cannot know which, so it is refused.
                    if (commentChar.Length != 1)
                        return Fail($"--comment-char needs one character, not \"{commentChar}\".", out error);
                    options = options with { CommentChar = commentChar[0] };
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                        return Fail($"Unknown option \"{arg}\".", out error);
                    if (pathGiven)
                        return Fail("Give one message file at most.", out error);
                    pathGiven = true;
                    path = arg == "-" ? null : arg;
                    break;
            }
        }

        parsed = new CliArguments(path, warningsAsErrors, options, false);
        return true;
    }

    private static bool TryValue(IReadOnlyList<string> args, ref int i, out string value, out string? error)
    {
        if (i + 1 >= args.Count)
        {
            value = string.Empty;
            return Fail($"{args[i]} needs a value.", out error);
        }

        value = args[++i];
        error = null;
        return true;
    }

    private static bool Fail(string message, out string? error)
    {
        error = message;
        return false;
    }
}
