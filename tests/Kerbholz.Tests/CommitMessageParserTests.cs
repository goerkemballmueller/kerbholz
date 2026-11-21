using Kerbholz;

namespace Kerbholz.Tests;

public class CommitMessageParserTests
{
    [Fact]
    public void AHeaderWithScopeAndMarkerGivesItsParts()
    {
        var message = CommitMessageParser.Parse("feat(api)!: drop the v1 endpoints");

        message.Type.ShouldBe("feat");
        message.Scope.ShouldBe("api");
        message.HasBreakingMarker.ShouldBeTrue();
        message.Description.ShouldBe("drop the v1 endpoints");
        message.IsBreaking.ShouldBeTrue();
    }

    [Fact]
    public void AHeaderWithoutScopeHasNoScope()
    {
        var message = CommitMessageParser.Parse("fix: handle an empty file");

        message.Scope.ShouldBeNull();
        message.HasBreakingMarker.ShouldBeFalse();
    }

    [Fact]
    public void EmptyParenthesesGiveAnEmptyScopeSoARuleCanNameIt()
    {
        CommitMessageParser.Parse("fix(): handle an empty file").Scope.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("Add the parser")]
    [InlineData("feat:no space")]
    [InlineData("feat (api): space before the scope")]
    [InlineData("")]
    public void AHeaderOfAnotherFormHasNoType(string header)
    {
        var message = CommitMessageParser.Parse(header);

        message.HasConventionalHeader.ShouldBeFalse();
        message.Description.ShouldBeNull();
        message.Header.ShouldBe(header);
    }

    [Fact]
    public void AColonWithNothingBehindItIsAnEmptyDescription()
    {
        var message = CommitMessageParser.Parse("feat: \n");

        message.HasConventionalHeader.ShouldBeTrue();
        message.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void BodyAndFootersAreSplitAtTheLastParagraph()
    {
        var message = CommitMessageParser.Parse(
            "fix(parser): keep the hash of a reference\n\nNote: the first paragraph stays body.\n\nSecond paragraph.\n\nCloses #42\nReviewed-by: Jo");

        message.Body.ShouldBe("Note: the first paragraph stays body.\n\nSecond paragraph.");
        message.Footers.ShouldBe([new Footer("Closes", "#42"), new Footer("Reviewed-by", "Jo")]);
    }

    [Fact]
    public void ALastParagraphThatDoesNotStartWithAFooterIsBody()
    {
        var message = CommitMessageParser.Parse("docs: explain the hook\n\nSee the README.\nRefs: #7");

        message.Body.ShouldBe("See the README.\nRefs: #7");
        message.Footers.ShouldBeEmpty();
    }

    [Fact]
    public void AFooterValueMaySpanSeveralLines()
    {
        var message = CommitMessageParser.Parse(
            "feat: read the config from one file\n\nBREAKING CHANGE: the settings moved\nfrom two files into kerbholz.json\nRefs: #3");

        message.Footers.ShouldBe([new Footer("BREAKING CHANGE", "the settings moved\nfrom two files into kerbholz.json"), new Footer("Refs", "#3")]);
        message.IsBreaking.ShouldBeTrue();
        message.HasBreakingMarker.ShouldBeFalse();
    }

    [Theory]
    [InlineData("BREAKING CHANGE", true)]
    [InlineData("BREAKING-CHANGE", true)]
    [InlineData("breaking change", false)]
    [InlineData("Breaking-Change", false)]
    public void OnlyTheCapitalBreakingChangeTokenCounts(string token, bool breaking)
    {
        var message = CommitMessageParser.Parse($"feat: x\n\n{token}: the API changed");

        message.Footers.Single().Token.ShouldBe(token);
        message.IsBreaking.ShouldBe(breaking);
    }

    [Fact]
    public void CommentLinesAndTheDiffBelowTheScissorsAreNotPartOfTheMessage()
    {
        const string raw = "feat: add the CLI\n\n# Please enter the commit message for your changes.\nThe body.\n"
            + "# ------------------------ >8 ------------------------\n# Do not modify or remove the line above.\ndiff --git a/x b/x\n";

        var message = CommitMessageParser.Parse(raw);

        message.Body.ShouldBe("The body.");
        message.Footers.ShouldBeEmpty();
    }

    [Fact]
    public void AnotherCommentCharacterIsHonoured()
    {
        var message = CommitMessageParser.Parse("; a comment\nfix: x\n\n#12 was the cause", ';');

        message.Header.ShouldBe("fix: x");
        message.Body.ShouldBe("#12 was the cause");
    }

    [Fact]
    public void WindowsLineEndsAndTrailingSpacesDoNotChangeTheParts()
    {
        var message = CommitMessageParser.Parse("\r\n\r\nfix: trim   \r\n\r\nBody line.  \r\n\r\n");

        message.Header.ShouldBe("fix: trim");
        message.SecondLine.ShouldBe(string.Empty);
        message.Body.ShouldBe("Body line.");
    }

    [Fact]
    public void AOneLineMessageHasNoSecondLine()
    {
        CommitMessageParser.Parse("chore: bump").SecondLine.ShouldBeNull();
    }
}
