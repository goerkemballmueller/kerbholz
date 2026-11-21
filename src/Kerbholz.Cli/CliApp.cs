using System.Text;
using Kerbholz.Rules;

namespace Kerbholz.Cli;

/// <summary>The command line without the console: input and output come in as readers and writers, so the tests run it as it runs in a hook.</summary>
public static class CliApp
{
    /// <summary>Exit code when the message passed (or was skipped).</summary>
    public const int Passed = 0;

    /// <summary>Exit code when the message failed the check; git then refuses the commit.</summary>
    public const int Failed = 1;

    /// <summary>Exit code for wrong arguments or a file that cannot be read, so a broken hook is not mistaken for a bad message.</summary>
    public const int Misused = 2;

    /// <summary>Runs one check and returns the exit code.</summary>
    public static int Run(IReadOnlyList<string> args, TextReader stdin, TextWriter stdout, TextWriter stderr)
    {
        ArgumentNullException.ThrowIfNull(stdin);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (!CliArguments.TryParse(args, out var parsed, out var error))
        {
            stderr.WriteLine($"kerbholz: {error}");
            stderr.WriteLine(CliArguments.Usage);
            return Misused;
        }

        if (parsed.Help)
        {
            stdout.WriteLine(CliArguments.Usage);
            return Passed;
        }

        string raw;
        try
        {
            // git writes the message in UTF-8 unless i18n.commitEncoding says otherwise; a BOM, if any, is honoured.
            raw = parsed.Path is null ? stdin.ReadToEnd() : File.ReadAllText(parsed.Path, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            stderr.WriteLine($"kerbholz: cannot read \"{parsed.Path}\": {ex.Message}");
            return Misused;
        }

        var report = new CommitLinter(DefaultRules.Create(), parsed.Options).Lint(raw);
        foreach (var finding in report.Findings)
            stdout.WriteLine(finding);

        if (report.Passes(parsed.WarningsAsErrors))
            return Passed;

        var errors = report.Findings.Count(f => f.Severity == Severity.Error);
        var warnings = report.Findings.Count - errors;
        stdout.WriteLine($"kerbholz: {errors} error(s), {warnings} warning(s). See https://www.conventionalcommits.org/en/v1.0.0/");
        return Failed;
    }
}
