# Contributing to OdinCore

Thank you for helping improve OdinCore.

OdinCore is intended to stay small, predictable, and useful in real-world ASP.NET Core applications. Contributions are welcome from developers of every experience level.

## Ways to contribute

You can help by:

- reporting bugs
- proposing features or API improvements
- improving documentation and examples
- adding tests
- improving performance
- improving async and cancellation behavior
- reviewing pull requests
- sharing real-world use cases

## Before opening a pull request

For significant API or behavioral changes, please open an issue first. This gives maintainers and contributors a chance to discuss the use case and avoid unnecessary breaking changes.

Small fixes, documentation improvements, and tests can usually go directly to a pull request.

## Development setup

Requirements:

- .NET 10 SDK
- Git

Clone and build:

```bash
git clone https://github.com/Amindada021/OdinCore.git
cd OdinCore
dotnet restore
dotnet build -c Release
dotnet pack -c Release
```

## Pull request guidelines

Please keep pull requests focused on one concern.

Before submitting:

1. Build the project in Release mode.
2. Make sure existing behavior remains compatible unless the change is intentionally breaking.
3. Add or update documentation when public APIs change.
4. Explain the problem, the proposed solution, and any trade-offs.
5. Avoid unrelated formatting or refactoring in the same pull request.

## Design principles

Changes should generally preserve these principles:

- low ceremony
- predictable request-to-result flow
- minimal dependencies
- ASP.NET Core friendly APIs
- async-first behavior for I/O
- backward compatibility where practical
- simple extension points rather than hidden magic

## Breaking changes

Breaking changes should be rare and clearly justified. If a breaking change is needed, open an issue first so the migration path and versioning can be discussed.

## Performance changes

For performance-focused contributions, include measurements or a reproducible benchmark when practical. Optimizations should not silently change behavior.

## Documentation

README examples should stay small enough to understand quickly and should compile conceptually against the public API.

## Questions and ideas

If you are unsure whether an idea belongs in OdinCore, open a GitHub issue. Discussion is welcome even if you do not plan to implement the change yourself.
