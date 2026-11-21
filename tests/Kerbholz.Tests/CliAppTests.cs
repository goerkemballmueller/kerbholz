using Kerbholz.Cli;

namespace Kerbholz.Tests;

public sealed class CliAppTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("kerbholz-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static (int Code, string Out, string Err) Run(string[] args, string stdin = "")
    {
        using var input = new StringReader(stdin);
        using var output = new StringWriter();
        using var errors = new StringWriter();
        var code = CliApp.Run(args, input, output, errors);
        return (code, output.ToString(), errors.ToString());
    }

    private string MessageFile(string text)
    {
        var path = Path.Combine(_dir, "COMMIT_EDITMSG");
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void AGoodMessageFilePassesSilently()
    {
        var (code, output, _) = Run([MessageFile("feat(cli): read the message file\n\n# Please enter the commit message\n")]);

        code.ShouldBe(CliApp.Passed);
        output.ShouldBeEmpty();
    }

    [Fact]
    public void ABadMessageFailsWithItsFindings()
    {
        var (code, output, _) = Run([MessageFile("Added the CLI")]);

        code.ShouldBe(CliApp.Failed);
        output.ShouldStartWith("error header-format: ");
        output.ShouldContain("1 error(s), 0 warning(s)");
    }

    [Fact]
    public void StandardInputIsReadWithoutAFileOrWithADash()
    {
        Run([], "fix: x").Code.ShouldBe(CliApp.Passed);
        Run(["-"], "nope").Code.ShouldBe(CliApp.Failed);
    }

    [Fact]
    public void AWarningFailsOnlyWhenAsked()
    {
        var file = MessageFile("fix: handle an empty file.");

        var lenient = Run([file]);
        lenient.Code.ShouldBe(CliApp.Passed);
        lenient.Out.ShouldStartWith("warning description-trailing-period: ");
        Run([file, "--warnings-as-errors"]).Code.ShouldBe(CliApp.Failed);
    }

    [Fact]
    public void OptionsReachTheCheck()
    {
        Run(["--types", "release, hotfix", "-"], "release: 1.2.0").Code.ShouldBe(CliApp.Passed);
        Run(["--max-header-length", "5"], "fix: x").Code.ShouldBe(CliApp.Failed);
        Run(["--comment-char", ";"], "; note\nfix: x").Code.ShouldBe(CliApp.Passed);
        Run(["--no-skip"], "Merge branch 'x'").Code.ShouldBe(CliApp.Failed);
    }

    [Fact]
    public void AMissingFileIsMisuseNotABadMessage()
    {
        var (code, _, errors) = Run([Path.Combine(_dir, "missing")]);

        code.ShouldBe(CliApp.Misused);
        errors.ShouldContain("cannot read");
    }

    [Theory]
    [InlineData("--max-header-length", "zero")]
    [InlineData("--max-header-length", "0")]
    [InlineData("--comment-char", "auto")]
    [InlineData("--types", " , ")]
    [InlineData("--unknown", "x")]
    public void WrongArgumentsAreMisuse(string option, string value)
    {
        var (code, _, errors) = Run([option, value]);

        code.ShouldBe(CliApp.Misused);
        errors.ShouldContain("Usage: kerbholz");
    }

    [Fact]
    public void AnOptionWithoutItsValueIsMisuse()
    {
        Run(["--types"]).Code.ShouldBe(CliApp.Misused);
    }

    [Fact]
    public void TwoFilesAreMisuse()
    {
        Run(["a", "b"]).Code.ShouldBe(CliApp.Misused);
    }

    [Fact]
    public void HelpPrintsTheUsage()
    {
        var (code, output, _) = Run(["--help"]);

        code.ShouldBe(CliApp.Passed);
        output.ShouldContain("Exit codes:");
    }
}
