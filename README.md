# Linguistics

Linguistics is a local-first language-learning desktop application for macOS and Windows. The shared C# core owns deterministic learning and state changes; Avalonia renders the desktop interface.

## Requirements

- .NET SDK 10.0.302 or a compatible 10.0 patch selected by `global.json`.
- macOS 14 or newer, or Windows 10 22H2 or newer.

## Build and run

```sh
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src/Linguistics.App --no-build
```

The app requires no account, backend, analytics service, or network connection at runtime.

## Project layout

- `src/Linguistics.App`: Avalonia desktop application and platform integration.
- `src/Linguistics.Core`: UI-independent deterministic domain code.
- `tests/Linguistics.Core.Tests`: core unit tests.
- `tests/Linguistics.App.Tests`: persistence and application-boundary tests.

Learner-profile storage locations, schema behavior, and deletion scope are documented in [`docs/storage.md`](docs/storage.md).
