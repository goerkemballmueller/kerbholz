using Kerbholz;
using Kerbholz.Rules;

namespace Kerbholz.Tests;

public class CommitLinterTests
{
    private static readonly CommitLinter Linter = new();

    private static string[] RuleIds(string raw) => [.. Linter.Lint(raw).Findings.Select(f => f.RuleId)];

    [Theory]
    [InlineData("feat: add the parser")]
    [InlineData("fix(cli)!: exit with 2 when the file is missing")]
    [InlineData("docs(readme): explain the hook\n\nWhy a hook and not CI: the author sees it before pushing.\n\nRefs: #5")]
    [InlineData("FEAT: types are not case sensitive")]
    [InlineData("chore: wait for it…")]
    public void AGoodMessageHasNoFindings(string raw)
    {
        Linter.Lint(raw).Findings.ShouldBeEmpty();
    }

    [Fact]
    public void AHeaderOfAnotherFormIsReportedOnceAndNothingElse()
    {
        RuleIds("Added the parser").ShouldBe(["header-format"]);
    }

    [Fact]
    public void AnEmptyMessageSaysSo()
    {
        Linter.Lint("# only a comment\n").Findings.Single().Message.ShouldBe("The message is empty.");
    }

    [Fact]
    public void AnUnknownTypeIsAnError()
    {
        var report = Linter.Lint("feature: add the parser");

        report.Findings.Single().RuleId.ShouldBe("type-allowed");
        report.HasErrors.ShouldBeTrue();
    }

    [Fact]
    public void ConfiguredTypesReplaceTheDefaults()
    {
        var linter = new CommitLinter(DefaultRules.Create(), new LintOptions { Types = ["release"] });

        linter.Lint("release: 1.2.0").Findings.ShouldBeEmpty();
        linter.Lint("feat: x").Findings.Single().RuleId.ShouldBe("type-allowed");
    }

    [Fact]
    public void EmptyParenthesesAndAnEmptyDescriptionAreErrors()
    {
        RuleIds("fix(): ").ShouldBe(["scope-not-empty", "description-not-empty"]);
    }

    [Fact]
    public void TheHeaderLengthCountsWhatAReaderSees()
    {
        var linter = new CommitLinter(DefaultRules.Create(), new LintOptions { MaxHeaderLength = 10 });

        // "fix: " + an emoji (two UTF-16 units) + "e" with a combining accent (two units) + "xyz": 10 characters to a reader, 12 to string.Length.
        linter.Lint("fix: \U0001F600e\u0301xyz").Findings.ShouldBeEmpty();
        linter.Lint("fix: 12345678").Findings.Single().RuleId.ShouldBe("header-max-length");
    }

    [Fact]
    public void ATrailingPeriodIsOnlyAWarning()
    {
        var report = Linter.Lint("fix: handle an empty file.");

        report.Findings.Single().Severity.ShouldBe(Severity.Warning);
        report.Passes().ShouldBeTrue();
        report.Passes(warningsAsErrors: true).ShouldBeFalse();
    }

    [Fact]
    public void AnEllipsisIsNoTrailingPeriod()
    {
        Linter.Lint("fix: first part...").Findings.ShouldBeEmpty();
    }

    [Fact]
    public void TheSecondLineMustBeBlank()
    {
        RuleIds("fix: x\nthe body starts too early").ShouldBe(["body-leading-blank"]);
    }

    [Fact]
    public void ALowerCaseBreakingChangeFooterIsAnError()
    {
        RuleIds("feat: x\n\nbreaking change: the config moved").ShouldBe(["breaking-change-capitals"]);
        Linter.Lint("feat: x\n\nBREAKING CHANGE: the config moved").Findings.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Merge branch 'feature/x' into develop")]
    [InlineData("Revert \"feat: add the parser\"\n\nThis reverts commit 77d4551.")]
    [InlineData("fixup! feat: add the parser")]
    [InlineData("squash! feat: add the parser")]
    [InlineData("amend! feat: add the parser")]
    public void MessagesGitWritesItselfAreSkipped(string raw)
    {
        var report = Linter.Lint(raw);

        report.SkippedBecause.ShouldNotBeNull();
        report.Findings.ShouldBeEmpty();
    }

    [Fact]
    public void SkippingCanBeSwitchedOff()
    {
        var linter = new CommitLinter(DefaultRules.Create(), new LintOptions { SkipGitGenerated = false });

        linter.Lint("Merge branch 'feature/x'").Findings.Single().RuleId.ShouldBe("header-format");
    }

    [Fact]
    public void ARuleGivenTwiceIsRefused()
    {
        Should.Throw<ArgumentException>(() => new CommitLinter([new HeaderFormatRule(), new HeaderFormatRule()], new LintOptions()));
    }

    [Fact]
    public void AnOwnRuleRunsBesideTheDefaults()
    {
        var linter = new CommitLinter([.. DefaultRules.Create(), new NoWipRule()], new LintOptions());

        RuleIds("chore: wip").ShouldBeEmpty();
        linter.Lint("chore: wip").Findings.Single().RuleId.ShouldBe("no-wip");
    }

    private sealed class NoWipRule : ICommitRule
    {
        public string Id => "no-wip";

        public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
        {
            if (message.Description is "wip")
                yield return new Finding(Id, Severity.Error, "Say what the commit does.");
        }
    }
}
