# Seven-Milestone Delivery Plan

## How to use this plan

This is a sequencing document, not implementation authorization. Before each milestone or separately requested feature, inspect the current repository and ask the user to select Token Saver or Full Assurance unless already specified. Resolve material product decisions one at a time with a recommendation.

Implement one observable vertical slice at a time. Keep the app compiling, preserve unrelated work, update affected canonical documents and mirror pages, and satisfy the milestone gate before starting the next milestone.

Every milestone ends in one truthful state: complete and verified, complete with named unverified evidence, blocked by a named dependency, paused at a clean resumption point, or proposal awaiting approval.

## Milestone 1 — Native foundation

### Status

Complete at commit `44bc423` with named unverified Windows interaction evidence. macOS onboarding, persistence, editing, deletion, keyboard, and VoiceOver behavior were exercised on the real app. GitHub Actions run `32120635585` passed locked restore, Release build, tests, and publish on macOS and Windows. Hands-on Windows and Narrator interaction remain unverified because no Windows desktop was available.

### Objective

Create the smallest shared macOS and Windows desktop shell that can onboard a multilingual learner, persist the profile locally, and reopen into a usable navigation structure. No curriculum, LLM, STT, or TTS is required.

### Small steps

1.1 Inspect the installed .NET toolchain, current Avalonia, macOS, and Windows platform guidance, and repository state.

1.2 Propose the minimum operating-system versions, product name, application-identifier strategy, and signing approach; obtain approval for choices that affect distribution.

1.3 Create a shared Avalonia desktop app and unit-test project targeting .NET 10, with Avalonia as the only production dependency family.

1.4 Establish only the source folders needed for the first vertical slice; do not pre-create empty speculative modules.

1.5 Add app dependency assembly so views receive protocol-backed domain owners rather than constructing services ad hoc.

1.6 Add the sidebar shell with Today, Learn, Scenarios, Pronunciation, Review, Progress, Languages, and Settings destinations.

1.7 Give unavailable destinations honest placeholder or empty states without implying implemented capability.

1.8 Define stable identifiers and initial domain models for learner profile, known language, target language, and user settings.

1.9 Model known languages as a collection with proficiency, reading comfort, listening comfort, and explanation consent.

1.10 Model multilingual shortcut behavior as automatic, ask first, one preferred language, or never.

1.11 Build the target-language onboarding step.

1.12 Build multi-select known-language onboarding.

1.13 Build per-language proficiency and explanation-preference steps.

1.14 Build the multilingual-routing preference step with plain-language explanations.

1.15 Offer microphone use now, later, or never without requesting permission prematurely.

1.16 Process microphone audio transiently and never retain it; explain that policy.

1.17 Select the simplest native local persistence mechanism that supports migration and repository tests; document the decision.

1.18 Implement a schema/version baseline and app-owned storage location.

1.19 Implement learner-profile create, read, update, and delete through `LearnerRepository`.

1.20 Restore the persisted profile on relaunch and route incomplete profiles back into onboarding.

1.21 Add Settings controls to edit languages and explanation preferences, plus legacy-audio cleanup without a retention choice.

1.22 Add the initial “Delete all learning data” path for the data that exists at this stage, with explicit confirmation and verified scope.

1.23 Add keyboard navigation, Narrator and VoiceOver labels, focus order, scalable text, and minimum contrast checks to onboarding and navigation.

1.24 Add unit tests for model validation, repository round trips, migration baseline, incomplete onboarding, and deletion.

1.25 Add UI-level evidence for onboarding, relaunch persistence, settings editing, permission-deferral, and deletion.

1.26 Create codebase-mirror pages only for important implemented files whose impact is not obvious.

### Acceptance gate

- A clean checkout builds and publishes the appropriate application target with the repository's documented commands on macOS and Windows.
- All tests pass.
- A learner can complete English + Hindi → German onboarding, quit, relaunch, and see the same local profile.
- Editing known languages and preferences persists.
- Deleting current learning data removes only app-owned learner data and returns to onboarding.
- No account, backend, analytics, inference, speech model, or network request is required.
- Keyboard, Narrator, and VoiceOver interaction are manually verified on the real app on their respective platforms, or missing platform evidence is named explicitly rather than inferred.
- Build, automated, visual, and interactive evidence are reported separately.

### Explicit exclusions

No curriculum graph, concept progress, transfer routing, model integration, audio capture, pronunciation assessment, or full release packaging.

## Milestone 2 — Deterministic curriculum core

### Status

Complete in the current uncommitted working tree on 2026-08-19, with named unverified Windows evidence. The pure C# core validates typed identifiers and concept graphs, applies versioned progression thresholds, routes approved English/Hindi mappings with structured rejection reasons, deterministically scores the next concept from an injected clock and seed, and composes a minimal lesson plan. Schema 2 persists progress, attempts, separate evidence dimensions, selected bridges, and configuration/content versions while reading schema 1 without rewriting it until a successful save. A developer-only Learn surface exercises synthetic data and explicitly disclaims teaching content. Release build, 57 tests, format, publish, macOS onboarding/diagnostics/keyboard interaction, schema-one restore, schema-two migration, storage inspection, and relaunch pass locally. The current snapshot has not run on Windows or Narrator because it has not been committed or pushed to CI and no Windows desktop is available.

### Objective

Prove that concepts, prerequisites, learner progress, transfer routing, next-concept selection, and lesson choice work reproducibly without an LLM.

### Small steps

2.1 Reconcile the initial domain-model proposal against implemented Milestone 1 code and simplify it.

2.2 Define typed IDs and validated language identifiers.

2.3 Implement `ConceptNode` with type, prerequisites, success criteria, error-rule references, and task tags.

2.4 Implement concept progression states: locked, available, introduced, practicing, provisionally mastered, review due, and mastered.

2.5 Define explicit allowed transitions and the evidence each transition consumes.

2.6 Implement `ConceptProgress` and `ConceptAttempt` with separate evidence dimensions.

2.7 Implement graph loading and prerequisite readiness.

2.8 Reject duplicate IDs, missing prerequisites, and cycles with attributable errors.

2.9 Define `TransferMapping` and its closed relation enum without embedding target-language facts in the mapping.

2.10 Define learner eligibility rules for explanation language and relevant skill preferences.

2.11 Agree on initial transfer scoring factors, threshold, and stable tie-breaking.

2.12 Implement `TransferRouter` as a pure deterministic function.

2.13 Return a structured routing explanation for developer diagnostics.

2.14 Define versioned next-concept scoring configuration.

2.15 Implement review urgency, prerequisite readiness, recurring error, task relevance, transfer opportunity, and cognitive-load inputs only where real data exists.

2.16 Implement stable tie-breaking and inject clock/seed inputs.

2.17 Implement a minimal `LessonComposer` that chooses from approved deterministic components.

2.18 Add repository support for concept progress, attempts, selected bridges, and configuration versions.

2.19 Add developer fixtures for fixed learner state and content.

2.20 Test every progression transition and rejection.

2.21 Test graph readiness, missing references, duplicates, and cycles.

2.22 Test Hindi selection, English selection, preference behavior, thresholds, no-bridge behavior, and invalid mappings.

2.23 Test identical input reproducibility for concept, bridge, task type, and selection explanation.

2.24 Test persistence round trips and migration from the Milestone 1 schema.

2.25 Add a small developer-only explanation surface or test-readable trace for selection decisions.

### Acceptance gate

- The app and all tests build without Ollama or speech dependencies.
- Fixed inputs return the same next concept and bridge across repeated runs.
- Invalid graph and mapping data fail clearly.
- The router can select reviewed English or Hindi mappings and can choose no mapping.
- No Avalonia view owns scoring or progression logic.
- Persistence keeps distinct evidence dimensions and content/configuration versions.
- Architecture, curriculum, transfer, and mirror documentation reflect implemented truth.

### Explicit exclusions

No broad German curriculum, runtime LLM, conversation scenario, STT, TTS, or completed review scheduler.

## Milestone 3 — Content system and first German pack

### Objective

Create a small, reviewed, versioned German content slice plus independent English-to-German and Hindi-to-German transfer packs, backed by a deterministic validator.

### Small steps

3.1 Confirm the exact learning goals and scenario domains for the first 10–20 concepts.

3.2 Finalize pack kinds, manifest fields, schema versioning, ID rules, dependency rules, and runtime-eligible review states.

3.3 Define serializable C# schemas for target concepts, transfer mappings, tasks, error rules, feedback templates, rubrics, and source records actually needed by the slice.

3.4 Create a target-language German pack independently of known languages.

3.5 Create separate English-to-German and Hindi-to-German transfer packs.

3.6 Create a universal or shared task-template area only for genuinely reusable data.

3.7 Establish source-manifest and license records before adding substantive content.

3.8 Research candidate concepts and record claim-level provenance.

3.9 Obtain competent linguistic review for target-language facts and cross-linguistic claims; machine generation alone is not approval.

3.10 Add approximately 10–20 concepts with explicit prerequisites and success criteria.

3.11 Add only the vocabulary, morphology, syntax, functions, examples, and phonology required by those concepts.

3.12 Add at least one approved English facilitative bridge.

3.13 Add at least one approved Hindi facilitative bridge.

3.14 Add at least one English interference warning.

3.15 Add at least one neutral, unknown, or intentional no-bridge case.

3.16 Add reviewed counterexamples and negative-transfer risks where relevant.

3.17 Create three to five task templates across approved everyday domains.

3.18 Give each task explicit states, allowed transitions, deterministic success conditions, and scripted fallback content.

3.19 Add deterministic error rules and concise feedback templates for the active concepts.

3.20 Add initial pronunciation/perception utterance metadata without claiming unsupported assessment.

3.21 Build the content validation utility in the simplest repository-native form.

3.22 Validate missing/duplicate IDs, cycles, broken references, language codes, schemas, CEFR values, transitions, evaluator coverage, provenance, review state, and license metadata.

3.23 Make invalid bundled content fail the applicable test or build gate.

3.24 Add positive fixtures plus one attributable failing fixture for each validation category.

3.25 Load only validated, compatible content into the runtime repository.

3.26 Display the tiny curriculum and transfer-note examples in a developer or learner-facing read-only path without adding conversation yet.

3.27 Review learner-facing German, English, and Hindi text in its rendered context.

3.28 Record third-party and source attributions required by the pack.

### Acceptance gate

- All bundled packs decode and validate deterministically.
- The prerequisite graph is acyclic and every reference resolves.
- The German core is not duplicated for English and Hindi learners.
- Required helpful, interfering, and no-bridge examples exist with review and provenance.
- Three to five task templates have deterministic state and success contracts.
- Corrupt fixtures fail with precise errors.
- The app can browse the validated slice without Ollama.
- Content and licenses are reviewed for the intended repository use; redistribution remains blocked where not yet approved.

### Explicit exclusions

No comprehensive A1 curriculum, runtime research generation, downloadable community packs, live NPC conversation, STT, or phoneme scoring.

## Milestone 4 — Local Ollama adapter

### Objective

Add an optional local dialogue renderer with strict schemas and deterministic fallbacks, without giving it teaching or state authority.

### Small steps

4.1 Recheck current official Ollama macOS and Windows, API, structured-output, streaming, and licensing guidance.

4.2 Define supported local-only endpoint rules and reject remote or cloud aliases in the default product mode.

4.3 Implement the smallest `LanguageModelProvider` domain contract required by the first task.

4.4 Implement local service health and version/capability diagnostics.

4.5 Query installed models and normalize only the metadata the app uses.

4.6 Add Settings UI for explicit model selection and a clear unavailable state.

4.7 Show model source, storage, license status, and capability evidence before recommending it.

4.8 Do not silently download a model; make download/setup a separately confirmed action.

4.9 Define the first closed response schema for NPC text, intent, proposed state, and allowed vocabulary IDs.

4.10 Build a minimal-context constructor containing only the current task facts, allowed values, and schema.

4.11 Add prompt and schema version identifiers.

4.12 Use low-variance, bounded runtime settings appropriate to structured tasks.

4.13 Decode transport and syntax errors separately.

4.14 Validate schema, enumerations, identifiers, length, and target-language constraints.

4.15 Pass schema-valid proposals to the deterministic task engine for semantic acceptance.

4.16 Implement request timeout and user cancellation.

4.17 Associate requests with session IDs so obsolete responses are discarded.

4.18 Add streaming for user-facing text only where it improves the experience; never mutate state from partial chunks.

4.19 Add scripted fallback output for service absence and every invalid-response path.

4.20 Add local diagnostics for model, request ID, durations, schema version, and validation result without logging prompt bodies by default.

4.21 Unit-test the consumer with a fake provider before depending on a local installation.

4.22 Test malformed JSON, missing/extra fields, invalid labels, unknown IDs, forbidden transitions, timeout, cancellation, stale responses, and fallback.

4.23 Run an authorized real local smoke test against every configuration claimed as supported.

4.24 Verify deterministic lessons still work when Ollama is stopped or absent.

### Acceptance gate

- The app builds and core learning runs with no Ollama service.
- A compatible local model can return a schema-valid bounded response.
- Unknown IDs and forbidden transitions are rejected.
- Partial, timed-out, cancelled, and stale responses never mutate task or learner state.
- A malformed response produces scripted recovery, not a weakened schema.
- Default logs and prompts respect the privacy boundary.
- Supported model claims and license implications have current evidence.

### Explicit exclusions

No remote inference provider, cloud model, autonomous tool use, dynamic curriculum generation, model-decided mastery, or speech input/output.

## Milestone 5 — Complete communicative task

### Status

Complete at pushed commit `3966a0d` on 2026-08-20. The café slice uses a pure three-state reducer, deterministic extraction/evidence/feedback, confirmed transfer routing, bounded optional dialogue realization, scripted recovery, schema-3 atomic persistence, and save retry. The normal bundled drafts still fail the runtime review/license gate; tests use synthetic approved resources only. Release build, publish, and 125 automated tests passed. Native macOS interaction was prepared in an isolated bundle but is not evidence because the Mac was locked; Windows, Narrator, competent linguistic review, and license approval are also unverified.

### Objective

Deliver one complete text-first scenario in which deterministic state controls the task, local generation makes the NPC natural when available, a focused correction enables retry, and progress is persisted.

### Small steps

5.1 Select one scenario with the user; recommend the smallest scenario that exercises multilingual transfer and two or three deterministic state transitions.

5.2 Freeze its acceptance envelope: actors, goal, context, success conditions, active concepts, data changes, failures, and evidence.

5.3 Finalize the scenario's task template, state enum, allowed transitions, and scripted dialogue path.

5.4 Implement the task reducer as a pure function.

5.5 Implement deterministic extraction for required slots and communicative functions where practical.

5.6 Add only the constrained classifier labels needed when deterministic extraction is insufficient.

5.7 Implement `TaskEvaluator` and prove that success conditions do not depend on free-form model judgment.

5.8 Build the task screen with goal, context, success criteria, conversation, text input, hint, repeat, translation on request, and exit.

5.9 Add clear loading, unavailable-model, invalid-response, error, cancellation, and recovery states.

5.10 Connect the validated Ollama response as optional NPC realization.

5.11 Connect the scripted NPC fallback so every required state remains completable without Ollama.

5.12 Apply the selected transfer bridge visibly and explain its source language.

5.13 Detect relevant deterministic error rules in learner input.

5.14 Rank communication-blocking, target-concept, repeated, pronunciation-placeholder, minor-form, and style feedback.

5.15 Display one primary micro-intervention in the ordinary case.

5.16 Let the learner retry while retaining scenario context.

5.17 Store communicative success, linguistic accuracy, fluency evidence, pronunciation evidence if any, and target-concept performance separately.

5.18 Update concept progress only through the deterministic curriculum engine.

5.19 Create the review handoff record without implementing the full scheduler.

5.20 Record which bridge and content/configuration versions were used.

5.21 Add developer inspection for current state, allowed transitions, evaluation evidence, prompt schema, proposal, and acceptance/rejection.

5.22 Unit-test every state and transition, including premature model completion.

5.23 Integration-test the full scripted path and valid/invalid provider paths.

5.24 Interactively verify task completion, correction, retry, fallback, exit, relaunch evidence, keyboard use, Narrator, and VoiceOver on the applicable platforms.

### Acceptance gate

- One scenario is completable from launch through persisted progress.
- Deterministic success works with Ollama unavailable.
- The local model can enrich dialogue but cannot skip conditions or invent IDs.
- At least one reviewed multilingual bridge is visible and explained.
- At least one targeted correction supports retry without ending the task.
- Communicative success and language dimensions are stored separately.
- Automated coverage, visual inspection, and genuine interaction evidence are separately reported.

### Explicit exclusions

No scenario library breadth, microphone input, synthesized NPC speech, full review experience, or phoneme-level assessment.

## Milestone 6 — Local speech and honest pronunciation

### Status

Committed and pushed as `61e60ad` on 2026-08-20, with named hardware/model gaps. The slice adds separate synthesis, recognition, and assessment contracts; deterministic German system-voice selection; normal/slower playback and stop; an explicitly configured `whisper-stream` process with fixed arguments, VAD, timeout, cancellation, first-transcript parsing, and no audio-save flag; point-of-use microphone disclosure; editable café transcripts that lose speech evidence when changed; a dedicated pronunciation screen; and schema-4 pronunciation metadata without transcripts or audio. The provider-absent path is the real local configuration because no usable German Whisper model is installed. Locked restore, zero-warning Release build, 146 tests, formatter, and publish pass. Native playback, microphone permission, real recognition, VoiceOver, Windows, Narrator, performance profiles, and model redistribution evidence remain unverified; the Mac was locked during attachment and no model download was authorized.

### Objective

Add replaceable local TTS and STT to the completed task, plus an honest first pronunciation experience focused on intelligibility and accessible fallbacks.

### Small steps

6.1 Recheck current Apple and Windows speech APIs, upstream `whisper.cpp` integration guidance, supported operating-system versions, licenses, model terms, and redistribution constraints.

6.2 Finalize `SpeechSynthesisProvider`, `SpeechRecognitionProvider`, and `PronunciationAssessmentProvider` domain contracts from actual slice needs.

6.3 Implement replaceable system TTS adapters using current supported Apple and Windows APIs.

6.4 Enumerate installed voices appropriate to German and expose unavailable-state diagnostics.

6.5 Add speak, stop, pause/resume if needed, cancellation, replay, and adjustable rate.

6.6 Implement seeded voice selection for high-variability perception.

6.7 Degrade gracefully when fewer appropriate voices are installed.

6.8 Add captions and keyboard-accessible playback controls.

6.9 Request microphone permission only when the learner initiates or explicitly opts into speech.

6.10 Build a visible bounded recording session with stop, cancel, timeout, interruption, silence, and missing-device handling.

6.11 Normalize captured audio locally into the adopted STT format.

6.12 Integrate `whisper.cpp` behind the provider boundary using the smallest maintainable packaging approach.

6.13 Make speech-model acquisition explicit with source, size, storage, capability, and license disclosure.

6.14 Return transcript and only genuinely supported timing metadata.

6.15 Add language configuration appropriate to the task and tested model.

6.16 Connect transcription to the same deterministic task-evaluation path used by text.

6.17 Protect against late transcription and synthesis callbacks with request/session identity.

6.18 Delete any provider-created temporary audio after success, failure, cancellation, and app relaunch cleanup.

6.19 Do not implement retained-recording storage. A future policy change requires separate explicit approval, privacy review, and deletion semantics before any product work.

6.20 Keep “Delete speech recordings” as independent cleanup for app-owned legacy or temporary audio files.

6.21 Implement the first pronunciation result using intelligibility, expected-versus-recognized words, duration, and supported timing only.

6.22 Keep phoneme scores, articulator diagnoses, accent percentages, and native-likeness absent.

6.23 Rank pronunciation feedback according to communication impact.

6.24 Add text-only and microphone-free equivalents to every speech step.

6.25 Test permission denial, silence, missing model, missing voices, provider failure, cancellation, stale callbacks, and temporary-file cleanup.

6.26 Test deterministic voice selection and graceful variation limits.

6.27 Measure latency and memory on approved low-resource and balanced hardware profiles before making quality claims.

6.28 Interactively verify microphone, transcript correction or retry, NPC playback, captions, rate, transient-audio cleanup, deletion, keyboard, Narrator, and VoiceOver on the applicable platforms.

### Acceptance gate

- The complete task accepts local speech and produces local NPC speech when providers are available.
- Text-only completion remains fully functional.
- Microphone denial and provider absence have clear recovery paths.
- Default recordings and temporary files are demonstrably deleted.
- Microphone audio is never retained; app-owned legacy or temporary audio files can be deleted independently.
- Voice variation is deterministic and honest about installed inventory.
- Pronunciation feedback contains no unsupported precision.
- Dependency, model, and distribution licenses are reviewed for the tested configuration.

### Explicit exclusions

No cloud speech, perfect pronunciation grading, forced alignment, phoneme scoring without a validated model, or promise of native-like accent.

## Milestone 7 — Review, progress, hardening, and release readiness

### Status

Review/progress is committed and pushed as `d182749`; local-data/release hardening is `34d84fd`; the final-design slice is `f8cde75`. `review-v1` uses explicit clock/configuration, response latency, difficulty, bounded intervals, and stable item IDs; task, concept, pronunciation, and recurring-error evidence synchronize without duplication. Today, Review, and Progress share one atomic controller. Concept review uses delayed recall plus already-stored communicative evidence, never inventing task success. Schema 5 persists review state and tests byte-for-byte read-only migration from schemas 1–4. Hardening includes byte-preserving recovery, fixed-field redacted local logging, expanded developer inspection, exact deletion, dependency audits, publish notices, and explicit release blockers. The final-design slice centralizes light/dark tokens, polished feature surfaces, short purposeful motion, a saved and pre-profile reduced-motion path, and automated semantic contrast/resource checks. Locked restore, zero-warning Release build, 171 tests, formatter, framework-dependent publish, notice hashes, and artifact-scope inspection pass locally. GitHub Actions run `32342596021` passes restore, Release build, tests, and publish on macOS and Windows for exact commit `f8cde752309e50fe3bc3d1a7a4c490562e497c29`; native platform/accessibility interaction remains gated by the locked Mac and unavailable Windows desktop.

Paper design system v2 Phase 1 is complete in the current uncommitted working tree on 2026-08-30. It adds paired paper materials, tape/stamp/torn-edge controls, tested stepped choreography with skip and reduced-motion final states, a fixed nine-layer PaperStage, a generated raster-cutout developer sandbox, and paper treatment for shell, Today, Progress, and Settings. New microphone audio is not retained and the learner-facing retention preference has been removed while keeping schema compatibility. Two consecutive independent visual-QA passes are clean in both themes. Current real keyboard and VoiceOver interaction remains named unverified because the macOS process lacks Accessibility control permission; Windows native interaction is also unavailable.

Paper design system v2 Phase 2 is complete in the same uncommitted working tree on
2026-08-30. It adds schema-2 lesson presentation contracts, attributable validation,
deterministic authored/fallback course projection, an app-only renderer registry and
developer gallery, and object-spotlight, picture-match, and word-order-train renderers.
The real German café-items lesson exercises all three as machine-validated preview
content without writing mastery. Release build, 231 tests, formatting, real macOS
mouse/keyboard interaction, text-only and reduced-motion fallbacks, both themes, and
two consecutive post-fix Codex plus Gemini visual-QA passes are clean. Direct VoiceOver
remains unverified because Accessibility trust is false; Windows native interaction is
unavailable.

### Objective

Complete the deterministic review loop, present capability-based progress, harden privacy and failure behavior, prove the full MVP journey, and prepare auditable macOS and Windows release paths.

### Small steps

7.1 Finalize the configurable review algorithm and evidence inputs.

7.2 Implement review items for words, phrases, concepts, listening contrasts, pronunciation targets, and recurring errors actually used by the MVP.

7.3 Persist last-seen, due time, streak, failures, difficulty, and latency only where the algorithm uses them.

7.4 Inject clock and configuration so scheduling tests are reproducible.

7.5 Build Today and Review queues with due, empty, loading, and recovery states.

7.6 Feed review outcomes back into concept progress deterministically.

7.7 Build Progress around communicative capabilities and secondary concept status.

7.8 Ensure raw lesson count, XP, currency, and punitive streaks do not become primary metrics.

7.9 Complete developer inspection for concept graph, prerequisites, mastery evidence, mappings, tasks, errors, review history, prompt schemas, and transitions.

7.10 Complete categorized local logging and default redaction.

7.11 Test database migrations from every released local schema version.

7.12 Test corrupted store recovery without silently discarding learner history.

7.13 Audit temporary audio files, legacy audio paths, caches, model locations, pack locations, and deletion scope.

7.14 Verify “Delete speech recordings” and “Delete all learning data” through before/after storage inspection.

7.15 Run the privacy and security review for endpoints, imports, content decoding, logs, paths, deletion, and asynchronous races.

7.16 Run the architecture review for dependency direction, view logic, provider leakage, and state authority.

7.17 Run the specification review against product principles and the complete acceptance envelope.

7.18 Run the unnecessary-complexity review and remove speculative abstractions, dependencies, and configuration.

7.19 Verify offline behavior after all required local artifacts are installed.

7.20 Verify Ollama absent, model missing, speech model missing, voice missing, microphone denied, corrupted provider output, cancellation, and low-resource states.

7.21 Complete keyboard navigation, Narrator, VoiceOver, scalable text, captions, focus order, contrast, and microphone-free verification across the MVP on the applicable platforms.

7.22 Run all unit, integration, content-validation, architecture, migration, and UI automation gates.

7.23 Perform genuine end-to-end interaction for onboarding -> selection -> bridge -> task -> speech/text -> correction -> completion -> progress -> review.

7.24 Freeze the release candidate and review the immutable snapshot.

7.25 Audit every bundled library, model, voice asset, dataset, and content source; generate required third-party notices.

7.26 Decide the authorized distribution channels and verify current Apple signing, entitlements, hardened-runtime, notarization, Windows code-signing, packaging, and privacy requirements.

7.27 Build and inspect the distribution artifact without bypassing signing or hooks.

7.28 Verify the artifact on a clean compatible Mac profile, including offline and first-run states.

7.29 Document supported macOS and Windows versions, hardware guidance, local dependencies, storage needs, setup, limitations, privacy, and deletion.

7.30 Reconcile canonical documentation, codebase mirror, known limitations, release checklist, and exact evidence.

7.31 Release or publish only with separate explicit authorization.

### Acceptance gate

- Deterministic review scheduling and progress survive relaunch and migration.
- The complete MVP loop works through text and, where available, local speech.

## Course expansion after the seven milestone foundation

The approved product direction now extends the small proof into a complete course experience. Delivery remains sliced because reviewed content cannot be safely manufactured in one implementation step.

### Course slice A: scalable catalog

- Project validated concepts, examples, and tasks into stable units, lessons, and cards.
- Target 450 lessons per language and reject catalogs above 500.
- Show authored versus planned capacity honestly.
- Prove deterministic ordering and the 500 lesson boundary.

### Course slice B: engaging learner experience

- Replace the Learn diagnostics dump with a course map and a focused lesson player.
- Use purposeful card transitions, clear progress, responsive layout, and reduced motion behavior.
- Keep authoring diagnostics available only in developer tooling.
- Verify keyboard, screen reader semantics, resize behavior, and real navigation.

### Course slice C: lesson history and resume

- Persist lesson visits, current card, completion, content version, and timestamps locally.
- Keep lesson completion separate from assessed mastery and review scheduling.
- Restore interrupted lessons and prove migration from existing learner schemas.

### Course slice D: bounded local model proposals

- Let a local model propose extra practice only from supplied approved IDs.
- Let it submit new lesson candidates only to a local draft review queue.
- Reject unknown references and invalid structures without changing learner state.
- Keep deterministic teaching fully usable with the model unavailable.

### Course slice E: reviewed content production

- Build language packs toward 450 lessons using traceable sources and independent review.
- Approve and publish only content with complete provenance and license evidence.
- Run linguistic, pedagogical, accessibility, and platform checks for every released language.
- Never describe planned or machine validated drafts as learner ready lessons.
- Offline, absent-provider, malformed-output, permission-denied, and low-resource behavior are verified.
- Privacy deletion and log redaction are proven through inspection.
- Accessibility is verified across the complete journey.
- All mandatory automated gates pass on the frozen release candidate.
- Architecture, specification, privacy/security, and complexity reviews have no unresolved blocking findings.
- Licenses, third-party notices, platform signing, notarization where applicable, artifact integrity, and clean-machine behavior are documented and verified for each chosen distribution method.
- Any actual public release has fresh explicit authorization.

### Explicit exclusions

No automatic expansion to A1–C2, cloud sync, hosted analytics, remote inference, community pack marketplace, teacher authoring, embeddings, semantic search, forced alignment, or advanced pronunciation model unless approved as a later roadmap feature.

## Requirements traceability

| Requirement group | Primary milestone | Continuing gate |
| --- | --- | --- |
| macOS and Windows desktop shell, onboarding, profile, settings, persistence | 1 | 5–7 |
| Deterministic curriculum, progress, transfer, selection | 2 | 3–7 |
| German core, English/Hindi mappings, tasks, provenance, validators | 3 | 5–7 |
| Optional local structured generation and fallback | 4 | 5–7 |
| Task-based interaction, focus on form, separate success dimensions | 5 | 6–7 |
| TTS, high variability, STT, intelligibility, recording privacy | 6 | 7 |
| Review, progress, explainability, privacy, accessibility, release | 7 | Release gate |

Future work must update this matrix only when the approved milestone boundary changes, not after every implementation detail.
