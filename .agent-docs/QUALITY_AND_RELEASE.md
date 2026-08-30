# Quality, Safety, and Release Guardrails

## Current repository state

The repository contains a .NET 10/Avalonia 12.1 solution, locked NuGet dependencies, MSTest projects, deterministic curriculum, lesson visit, task, speech, and review boundaries, schema six learner persistence with schema one through five migration, and a GitHub Actions macOS and Windows matrix. There is intentionally no signing or release configuration.

Repository gates are:

```sh
dotnet restore Linguistics.slnx --locked-mode
dotnet build Linguistics.slnx --configuration Release --no-restore
dotnet test Linguistics.slnx --configuration Release --no-build
dotnet format Linguistics.slnx --no-restore --verify-no-changes
dotnet publish src/Linguistics.App/Linguistics.App.csproj --configuration Release --no-restore
```

CI runs restore, build, test, and publish on both `macos-latest` and `windows-latest`. Formatting is currently a local Full Assurance gate.

This document defines required evidence categories and shipping boundaries until executable gates exist.

## Gate principles

- Use the smallest check that fully covers the change, then run every mandatory gate at its required stage.
- Prefer deterministic, local, reproducible, and attributable checks.
- Treat architecture and content checks as a ratchet; fix violations instead of adding exemptions.
- Never bypass hooks, signing, schema validation, privacy boundaries, or required tests to finish.
- A green check proves only what it exercises.
- Provider-present tests supplement rather than replace deterministic fake-provider tests.
- A release candidate is a frozen snapshot; review and evidence must identify its exact commit and artifact.

## Documentation-only gate

For changes limited to this local documentation set:

1. Confirm every canonical subject has one owner in `README.md`.
2. Confirm root `AGENTS.md` routes to every canonical document.
3. Confirm exactly seven milestone headings and no implementation claim.
4. Check relative Markdown paths and headings.
5. Search for contradictions in local-first, deterministic authority, privacy, mode selection, and shipping authorization.
6. Confirm current technical claims use primary references or are explicitly marked for re-verification.
7. Confirm all planning documents are ignored as requested.
8. Before a setup commit, confirm only `.gitignore` is staged.

Because the documents are ignored, this gate is manual and local. CI cannot detect their deletion or drift.

## Build and static gates

Once implementation exists, establish repository-owned commands for:

- Clean macOS and Windows app builds.
- Unit tests.
- Integration tests.
- Format or style checks if adopted.
- Static analysis and compiler warnings.
- Architecture dependency checks.
- Content validation.
- Migration tests.
- UI automation.

Prefer .NET SDK and NuGet capabilities already needed by the app. Add a tool only when its value, maintenance cost, license, and clean-checkout setup are documented.

## Domain gates

### Curriculum and routing

Require tests for graph validity, progress transitions, deterministic selection, stable tie-breaking, bridge eligibility, no-bridge behavior, separate evidence dimensions, and review scheduling.

### Content

Bundled content must fail the applicable gate for duplicate/missing IDs, cycles, broken references, invalid schemas, unsupported languages or CEFR values, invalid task transitions, missing evaluator coverage, missing required provenance, unapproved review state, or unresolved license status.

### Persistence

Require repository round trips, migration from every released schema, corrupted-store behavior, content-version traceability, deletion scope, and protection from late writes after profile deletion.

### Local model

Require fake-provider coverage for valid, malformed, unknown-ID, forbidden-transition, timeout, cancellation, stale-response, and fallback behavior. Add real local smoke evidence for each model configuration publicly claimed as supported.

### Speech

Require deterministic voice selection, missing voice/model behavior, permission denial, cancellation, stale callback protection, temporary-file cleanup, legacy-audio cleanup/deletion, and absence of unsupported assessment fields. Supplement automation with real hardware interaction.

## User-interface evidence

For an affected real route, consider:

- Loading.
- Success.
- Empty.
- Invalid content or provider response.
- Provider unavailable.
- Permission denied.
- Cancellation.
- Error and recovery.
- Relaunch persistence.
- Keyboard navigation.
- Narrator, VoiceOver, and focus order.
- Scalable text and captions.
- Low-resource or slow-provider behavior.

Report automated UI coverage, visual inspection, and genuine interaction separately. Screenshots do not prove controls, audio, persistence, deletion, or end-to-end state.

## Privacy and security gate

Mandatory review applies to changes involving learner data, persistence, migrations, microphone, recordings, model endpoints, downloads, imports, exports, logs, diagnostics, deletion, packaging, or distribution.

Review:

- Data minimization and ownership.
- Local-only endpoint enforcement.
- Permission timing and fallback.
- Temporary and retained file lifecycle.
- Log and diagnostic redaction.
- Typed path resolution and deletion scope.
- Untrusted content and model validation.
- Async race and stale response behavior.
- Secrets and personal data in source, fixtures, logs, and artifacts.
- Network disclosure and consent.
- Recovery from partial failure.

Never test deletion against real learner data unless the user explicitly authorizes the exact target and recovery plan.

## Dependency and license gate

Before adding or updating a dependency, model, dataset, voice asset, or content source:

1. Show why native or existing code is insufficient.
2. Identify the exact artifact and version.
3. Review maintenance, size, supported platforms, security posture, and update path.
4. Record license and redistribution terms.
5. Record required attribution or notices.
6. Confirm whether the artifact may be bundled or must be downloaded separately.
7. Verify checksum or integrity using the official distribution mechanism where available.
8. Add only with approval when it materially changes scope, app size, distribution, or network behavior.

Do not treat a model's presence in a catalog as permission to redistribute it.

## Branch, commit, and push

- Confirm the current branch and remote before work.
- Use an isolated branch or worktree when requested or when repository policy later requires it.
- Preserve unrelated user changes and never clean them destructively.
- Stage explicit paths and inspect the staged diff.
- Do not include ignored local planning documents in a commit unless the user reverses the ignore decision.
- Honor exact commit messages and authorship instructions.
- Do not add co-author trailers unless explicitly requested.
- Keep hooks enabled.
- Push only with explicit authority and verify the remote ref afterward.
- Opening a pull request, merging, tagging, publishing a release, or changing repository settings requires its own scope or explicit instruction.

## Distribution readiness

Local development success is not distribution readiness. Before direct distribution, verify current Apple guidance for macOS signing and notarization and current Microsoft guidance for Windows packaging and signing:

- <https://developer.apple.com/documentation/xcode/creating-distribution-signed-code-for-the-mac>
- <https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution>
- <https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-unpackaged-apps>
- <https://learn.microsoft.com/windows/msix/package/sign-app-package-using-signtool>

The chosen channel determines the exact gate. At minimum, review:

- Application identifier, version, and minimum operating-system versions.
- Apple and Windows signing identity and authority.
- Hardened runtime.
- Entitlements, especially microphone and file access.
- Usage descriptions and privacy disclosures.
- Every nested executable, framework, model, and resource.
- Third-party licenses and notices.
- Archive and package integrity.
- Notarization, MSIX, direct-download, or store requirements.
- Clean-machine launch and Gatekeeper behavior.
- Local model and speech setup on first run.
- Offline behavior after setup.
- Upgrade, migration, rollback, and deletion.
- Supported hardware and storage guidance.

Do not use ad hoc or test signing as evidence for an authorized public release. Do not expose signing credentials or notarization secrets.

## Release evidence bundle

For an approved release, record:

- Exact commit and version.
- Build environment and documented commands.
- Automated gate results.
- Content-pack and schema versions.
- Migration results.
- Supported model and speech configurations actually tested.
- Accessibility results.
- Privacy and security review.
- Dependency/license inventory and notices.
- Artifact checksum and structural inspection.
- Signing and notarization results for the chosen channel.
- Clean-machine and offline interaction evidence.
- Known limitations and unverified configurations.

Keep secrets, recordings, personal data, and full provider payloads out of the evidence bundle.

## Drift prevention

The current ignored documentation cannot be protected by repository hooks or CI. Local prevention consists of the canonical ownership table, root routing map, milestone traceability table, codebase-mirror reconciliation, and the documentation-only gate above.

If the user later wants the operating model shared or enforceable, propose a separate change to:

- Remove the relevant ignore rules.
- Commit the approved canonical documents.
- Add a small intent-based policy check for required routing, mode selection, deterministic authority, privacy, and shipping authorization.
- Run the check in local required gates and CI.

Do not silently make that migration.

## Shipping stop conditions

Stop before commit, push, or release if:

- The staged set contains unrelated or unexplained files.
- A required gate fails.
- Content provenance or license status is unresolved for a bundled artifact.
- Model or speech output can mutate state without validation.
- Privacy deletion or log-redaction evidence is missing.
- Signing or release authority is missing.
- The artifact differs from the reviewed snapshot.
- A claim lacks the required direct evidence.

Report the exact blocker and leave a clean resumption point.
