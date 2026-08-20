# Release readiness

This repository can build and publish a framework-dependent desktop artifact, but it is not authorized or ready for public distribution. The exact release blockers are listed below so a local build cannot be mistaken for a signed product.

## Proposed compatibility envelope

The release candidate should target macOS 14–26 on Apple silicon and Intel, Windows 10 22H2 on x64, and Windows 11 22H2 or later on x64/ARM64. This is a conservative proposal based on the current [.NET 10 macOS support table](https://learn.microsoft.com/en-us/dotnet/core/install/macos), [.NET 10 Windows support table](https://learn.microsoft.com/en-us/dotnet/core/install/windows), and [Avalonia support tiers](https://docs.avaloniaui.net/docs/supported-platforms). It is not yet the app's verified support claim: clean-device installation, first run, text-only use, speech, deletion, scalable text, keyboard navigation, VoiceOver, and Narrator remain to be exercised on every advertised platform/architecture.

The current `dotnet publish` output is framework-dependent and therefore requires the matching .NET 10 runtime, as described by Microsoft's [framework-dependent publishing documentation](https://learn.microsoft.com/en-us/dotnet/core/tutorials/publish-console-app). A public release must choose and test framework-dependent installers or per-runtime self-contained packages; the repository intentionally does not make that distribution decision implicitly.

## Local requirements

- Core text learning: app binary, .NET 10 runtime for the current artifact, and a runtime-approved bundled content pack.
- Optional dialogue variation: an already-installed local Ollama service and explicitly selected non-cloud model. Scripted dialogue remains complete without it.
- Optional transcription: an explicitly installed `whisper-stream` executable plus an explicitly configured compatible model. No model is downloaded or bundled.
- Optional playback: an installed German system voice. Captions and text remain complete without one.
- Storage: the learner JSON document and redacted log are small for the MVP. Optional model storage is external and model-dependent. The current speech adapter retains no audio.

## Distribution paths requiring a decision

For direct macOS distribution, Apple requires a valid Developer ID signature, hardened runtime, secure timestamp, and notarization; the result should be stapled and verified. See Apple's [notarization requirements](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution) and [Hardened Runtime guidance](https://developer.apple.com/documentation/security/hardened-runtime). The repository has no certificate, signing identity, entitlements file, notarization credential, or signed artifact.

For Windows, choose either Microsoft Store MSIX distribution, where Microsoft signs an accepted package, or an authorized off-Store signing method. Microsoft's current [Windows code-signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options) describe Store signing, Azure Artifact Signing, and certificate-based alternatives. The repository has no MSIX manifest, publisher identity, signing credential, or signed installer.

## Release-blocker ledger

| Area | Current evidence | Required before a public release |
| --- | --- | --- |
| Product code | Locked restore, Release build, automated tests, formatter, and framework-dependent publish run locally | Freeze an exact commit and rerun every gate on that snapshot |
| Content | Bundled packs are machine-validated drafts and fail the runtime gate | Competent linguistic/pedagogical approval and license/redistribution review |
| App license | No repository-level product license exists | Owner selects and adds the intended application license |
| Runtime notices | Resolved runtime packages audited; notices copied into publish output | Re-audit the exact frozen dependency graph and artifact |
| macOS | Local build/publish available; no current native interaction evidence for the final candidate | Package, Developer ID sign, hardened-runtime test, notarize, staple, Gatekeeper-check, clean-profile test, VoiceOver test |
| Windows | CI definition builds/tests/publishes on Windows; no native final-candidate interaction evidence | Choose packaging, sign, install on clean supported Windows devices, test Narrator, microphone denial, speech, and deletion |
| Models | Ollama and whisper.cpp remain external; no model weights bundled | Review each recommended model's source, exact hash, size, license, hardware profile, and redistribution status |
| Privacy | Local-only boundaries, redacted logs, scoped deletion, recovery, and migrations have automated evidence | Inspect storage before/after real UI deletion and permission-denial flows on both platforms |
| Accessibility | Controls carry names, headings, live regions, captions, and text-only alternatives | Complete keyboard, scalable-text, contrast, VoiceOver, and Narrator journeys on the frozen artifact |
| Authorization | Local commits and pushes are authorized | Obtain separate explicit authorization for any release, upload, Store submission, or deployment |

## Clean-machine acceptance journey

On each chosen OS/architecture: install without development tools; launch offline; complete onboarding; inspect local storage; run selection, bridge, café task, text fallback, optional speech, correction/retry, completion, Today, Progress, and Review; relaunch; deny microphone; stop all providers; corrupt a disposable test store and recover it; delete recordings; delete all learning data; verify app-owned recovery and diagnostics are gone while content, models, and unrelated files remain; run the platform screen reader; uninstall; verify no claim exceeds the captured evidence.
