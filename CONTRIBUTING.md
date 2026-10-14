# Contributing to Mortar

## Build and test

    dotnet build
    dotnet test

CI runs the tests on Windows, Linux, and macOS. All three must produce identical snapshots.

## Determinism rules

Mortar promises the same layout for the same inputs and seed on every platform. Changes to `Mortar.Core` must keep these rules:

- Use `Pcg32` for every random decision. Never use `System.Random`.
- Never let hash-set or dictionary enumeration order drive a decision. Build candidate lists from ordered lists, and sort by id with `StringComparer.Ordinal`.
- Use integer arithmetic for decisions. Floating point is only for turning home-region fractions into slot indices.
- Pass the same `Pcg32` through generation in a fixed order; adding or removing a random call changes every layout after it.

## Snapshots

`tests/Mortar.Tests/Snapshots` holds text renderings of the sample crypt for fixed seeds. If you change generation on purpose, delete the affected snapshot files, run `dotnet test` once to recreate them, review the new maps, and commit them with your change.

## Style

- Keep files under 300 lines and functions under 50 lines.
- The library returns results for authoring errors and generation failures; it does not throw for them.
- Commit messages are short, one line, and present tense, for example `add corridor rooms to sample`.
