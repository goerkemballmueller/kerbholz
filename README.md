# Kerbholz

A small linter for [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) messages, written in C# for .NET 10: a library
that parses and checks a message, and a command that runs as git's `commit-msg` hook.

## The name

A *Kerbholz* (tally stick) was how debts were recorded in medieval Europe: notches were cut across a stick, then the stick was split
lengthwise and each party kept one half. Only the two halves laid side by side proved the record, so neither side could change it alone.
A project's commit history is a record of the same kind, and Kerbholz keeps its notches readable.

## Use it as a hook

```sh
dotnet pack src/Kerbholz.Cli -c Release -o artifacts
dotnet tool install --global Kerbholz.Cli --add-source artifacts
```

Then, in the repository to check, `.git/hooks/commit-msg` (made executable):

```sh
#!/bin/sh
exec kerbholz "$1" --comment-char "$(git config core.commentChar || echo '#')"
```

git hands the hook the message file before it removes the comment lines, so Kerbholz drops them itself, and everything below the
scissors line of `git commit --verbose`. A refused commit keeps its message in `.git/COMMIT_EDITMSG`:
`git commit --edit --file .git/COMMIT_EDITMSG` opens it again.

## Use it from the command line

```sh
git log -1 --format=%B | kerbholz -
kerbholz message.txt --types feat,fix,docs --max-header-length 72 --warnings-as-errors
```

Exit codes: `0` passed, `1` the message has errors, `2` wrong arguments or an unreadable file. `kerbholz --help` lists the options.

## Rules

| Id | Severity | What it checks |
|---|---|---|
| `header-format` | error | the header reads `type(scope)!: description`, with a space after the colon |
| `type-allowed` | error | the type is one of the allowed ones (default: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert), in any case |
| `scope-not-empty` | error | parentheses hold a scope |
| `description-not-empty` | error | something follows the colon |
| `header-max-length` | error | the header has at most 100 characters, counted as a reader counts them (an emoji is one) |
| `description-trailing-period` | warning | the description does not end with a period (an ellipsis is fine) |
| `body-leading-blank` | error | the second line is blank |
| `breaking-change-capitals` | error | a `BREAKING CHANGE` footer is written in capitals; in any other case it marks nothing |

Merges and reverts git writes itself and `fixup!`/`squash!`/`amend!` commits pass unchecked (`--no-skip` checks them too).

## Use it as a library

```csharp
var report = new CommitLinter().Lint(File.ReadAllText(".git/COMMIT_EDITMSG"));
foreach (var finding in report.Findings)
    Console.WriteLine(finding);
```

A rule of your own implements `ICommitRule` and goes into the list: `new CommitLinter([.. DefaultRules.Create(), new MyRule()], new LintOptions())`.

## Build

```sh
dotnet test Kerbholz.slnx
```

## How it was written

The code was written with an AI coding agent (Claude); every commit says so in a `Co-authored-by` trailer.

## License

MIT, see [LICENSE](LICENSE).
