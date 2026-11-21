using System.Globalization;

namespace Kerbholz.Rules;

/// <summary><c>header-format</c>: the header must read <c>type(scope)!: description</c>.</summary>
public sealed class HeaderFormatRule : ICommitRule
{
    /// <inheritdoc/>
    public string Id => "header-format";

    /// <inheritdoc/>
    public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.HasConventionalHeader)
            yield break;
        yield return new Finding(Id, Severity.Error, message.Header.Length == 0
            ? "The message is empty."
            : "The header must read \"type(scope): description\", for example \"fix(parser): keep the hash of a reference\"; a space must follow the colon.");
    }
}

/// <summary><c>type-allowed</c>: the type must be one of <see cref="LintOptions.Types"/>.</summary>
public sealed class TypeAllowedRule : ICommitRule
{
    /// <inheritdoc/>
    public string Id => "type-allowed";

    /// <inheritdoc/>
    public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        if (message.Type is { } type && !options.Types.Contains(type, StringComparer.OrdinalIgnoreCase))
            yield return new Finding(Id, Severity.Error, $"The type \"{type}\" is not one of {string.Join(", ", options.Types)}.");
    }
}

/// <summary><c>scope-not-empty</c>: parentheses without a scope are a typo, not a scope.</summary>
public sealed class ScopeNotEmptyRule : ICommitRule
{
    /// <inheritdoc/>
    public string Id => "scope-not-empty";

    /// <inheritdoc/>
    public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.Scope is { } scope && string.IsNullOrWhiteSpace(scope))
            yield return new Finding(Id, Severity.Error, "The parentheses hold no scope: name one or leave them out.");
    }
}

/// <summary><c>description-not-empty</c>: something must follow the colon.</summary>
public sealed class DescriptionNotEmptyRule : ICommitRule
{
    /// <inheritdoc/>
    public string Id => "description-not-empty";

    /// <inheritdoc/>
    public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.Description is { } description && string.IsNullOrWhiteSpace(description))
            yield return new Finding(Id, Severity.Error, "Nothing follows the colon: say what the commit does.");
    }
}

/// <summary><c>header-max-length</c>: the header must fit <see cref="LintOptions.MaxHeaderLength"/>, counted as a reader counts characters.</summary>
public sealed class HeaderMaxLengthRule : ICommitRule
{
    /// <inheritdoc/>
    public string Id => "header-max-length";

    /// <inheritdoc/>
    public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        // UTF-16 length would count an emoji as two and a letter built from a base and a combining accent as two; a reader sees one.
        var length = new StringInfo(message.Header).LengthInTextElements;
        if (length > options.MaxHeaderLength)
            yield return new Finding(Id, Severity.Error, $"The header has {length} characters; at most {options.MaxHeaderLength} are allowed.");
    }
}

/// <summary><c>description-trailing-period</c>: a header is a title, not a sentence.</summary>
public sealed class DescriptionTrailingPeriodRule : ICommitRule
{
    /// <inheritdoc/>
    public string Id => "description-trailing-period";

    /// <inheritdoc/>
    public IEnumerable<Finding> Check(CommitMessage message, LintOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        // An ellipsis ("…" or "...") is left alone: it says the title goes on in the body, which is a choice, not a typo.
        if (message.Description is { } description && description.EndsWith('.') && !description.EndsWith("...", StringComparison.Ordinal))
            yield return new Finding(Id, Severity.Warning, "The description ends with a period; leave it out.");
    }
}
