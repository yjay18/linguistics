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

Milestones 1–3 provide the cross-platform desktop shell, a seven-step local onboarding flow for German with optional English and Hindi support, editable local profile settings, exact-scope learner-data deletion, a deterministic curriculum core, and strictly validated versioned content packs. The first compact German pack and separate English-to-German and Hindi-to-German transfer packs are machine-validated review drafts. Runtime teaching rejects them until named linguistic and license reviewers approve them; the application does not silently treat generated content as learner-ready.

Set `LINGUISTICS_DEVELOPER_MODE=1` when running locally to make the Learn destination show the approval-gated content browser plus a synthetic selection, routing, and composition explanation. Neither developer surface changes learner progress.

## Project layout

- `src/Linguistics.App`: Avalonia desktop application and platform integration.
- `src/Linguistics.Core`: UI-independent deterministic domain code.
- `content`: versioned target-language and per-source-language transfer packs.
- `tests/Linguistics.Core.Tests`: profile, curriculum, content-decoding, and validator unit tests.
- `tests/Linguistics.App.Tests`: persistence and application-boundary tests.

Learner-profile storage locations, schema behavior, and deletion scope are documented in [`docs/storage.md`](docs/storage.md).
Content provenance and current redistribution gates are documented in [`docs/content-license.md`](docs/content-license.md).
