# Security policy

## Supported versions

Only the newest commit on `main` is supported; there are no release branches.

## What Kerbholz does

Kerbholz reads one commit message (a file or standard input) and prints what it finds. It opens no network connection, runs no other
program and writes no file. The CI workflow runs with read-only repository permissions and passes event values through the environment,
never into the script text.

## Reporting a vulnerability

Please do not open a public issue. Use **Report a vulnerability** on this repository's **Security** tab (GitHub's private vulnerability
reporting). Every report is read; the answer says what was found and whether and when a fix follows.
