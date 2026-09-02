# Architecture

## Central boundary

The deterministic layer is the teacher. The generative layer is a replaceable renderer and conversational actor.

| Deterministic layer owns | Generative layer may do |
| --- | --- |
| What and when to teach | Render an approved NPC role naturally |
| Prerequisites and concept readiness | Produce bounded dialogue variations |
| Transfer mapping and bridge selection | Paraphrase an approved explanation |
| Task state and permitted transitions | Propose one allowed intent or state label |
| Communicative success | Generate examples from supplied concepts |
| Error severity and feedback priority | Assist classification when deterministic parsing is impractical |
| Mastery and review scheduling | Stream user-facing language |
| Pronunciation evidence and score meaning | Never invent or own a pronunciation score |
| Persistence mutations | Never mutate learner state directly |

A model response is untrusted input. It must match a supplied schema, reference only supplied identifiers, pass semantic validation, and then be accepted or rejected by deterministic code.

## Architectural layers

### Application shell

Owns app lifecycle, dependency assembly, window and scene configuration, navigation, and developer-mode entry points. It contains no educational rules.

The shell also applies one centralized light/dark token system and the learner's reduced-motion preference. Motion changes presentation only; it never gates an intent, state transition, live status, or educational result. `LINGUISTICS_REDUCED_MOTION=1` provides the same presentation override before a profile exists.

### Core domain

Owns value types, identifiers, explicit errors, configuration, clocks and seeds used for reproducibility, and provider contracts. It does not import Avalonia or concrete provider implementations.

### Curriculum

Owns concept graphs, prerequisite readiness, concept selection, transfer routing, task evaluation, focus-on-form ranking, lesson composition, mastery evidence, and review scheduling. Prefer pure functions and immutable inputs.

### Content

Owns bundled and installed versioned packs, schema decoding, dependency resolution, provenance, licenses, and validation. Content is read-only at runtime; learner data never lives inside a content pack.

It also projects validated concepts, examples, and tasks into a deterministic course catalog. The projection targets 450 lessons per language, accepts no more than 500, and reports the honest gap between authored and planned content. Authoring preview catalogs remain visibly distinct from runtime approved catalogs.

### Asset system

`tools/AssetPipeline` owns networked image research and authoring. Only its `search` and
`fetch` commands may contact Wikimedia Commons; they use `HttpClient`, a descriptive
User-Agent, conservative license filtering, bounded downloads, sequential requests, and
backoff. Processing, generated-image import, and audit are local operations. The tool is
not referenced by `Linguistics.App` and must not appear in app publish output.

`Linguistics.Core.Content` owns the strict `assets.json` schema, provenance and license
records, path/size/hash validation, per-template and pack budgets, and lesson-reference resolution. A
catalog exposes only validated immutable asset records and absolute local paths. Pending
asset review keeps the same Preview/runtime gate as the adjacent content pack.

`Linguistics.App.Content.ContentImageCache` owns lazy local decode and disposal. It has
no HTTP or provider dependency. Its cache key includes pack ID, pack version, and asset
ID so a new immutable pack version cannot reuse stale decoded bytes. Template renderers,
the developer PaperStage, and Settings receive this one read-only cache from the app
shell. Decode failure produces the authored text fallback; it does not trigger a fetch.
Settings lists every validated asset credit, and each rendered scene exposes the credits
for the images it actually presents.

### Local AI

Owns Ollama discovery, model configuration, constrained prompting, streaming, cancellation, timeouts, response decoding, and diagnostics. It has no authority over curriculum or persistence.

### Speech

Owns microphone capture, audio normalization, local transcription, local synthesis, voice selection, speech permissions, and evidence-based pronunciation assessment. Provider details stay behind protocols.

### Persistence

Owns local learner history, repositories, migrations, deletion, and transaction boundaries. Content versions referenced by attempts remain traceable without rewriting prior history.

The app-level persistence boundary writes one schema-versioned learner envelope atomically through a temporary sibling. Unsupported, corrupt, unfinished, linked, or oversized stores fail closed. An explicit startup action can preserve unreadable bytes under an app-owned randomized recovery name before a fresh profile is created. The coordinated all-data deletion path removes app-owned recordings and its fixed-field diagnostic log before the learner envelope and matching recovery copies; models, content packs, and unrelated siblings remain separate.

### Features

Avalonia features render state and send user intents to domain owners. Views must not calculate mastery, choose bridges, schedule review, parse model output, or directly manipulate persistence.

## Minimum service boundaries

Define protocols only when an approved vertical slice needs them. The anticipated boundaries are:

- `LanguageModelProvider`
- `SpeechRecognitionProvider`
- `SpeechSynthesisProvider`
- `PronunciationAssessmentProvider`
- `CurriculumRepository`
- `LearnerRepository`
- `LessonEngine`
- `ReviewScheduler`
- `TransferRouting`
- `TaskEvaluator`

Protocols describe capabilities and domain results, not a vendor's response shape. Do not create a protocol solely for speculative flexibility.

## Core domain entities

The first vertical slices are expected to require:

- `LearnerProfile`
- `KnownLanguage`
- `TargetLanguage`
- `ConceptNode`
- `ConceptProgress`
- `ConceptAttempt`
- `TransferMapping`
- `TaskTemplate`
- `TaskState`
- `TaskAttempt`
- `ErrorRule`
- `ErrorHistory`
- `ReviewItem`
- `ReviewSchedule`
- `VocabularyState`
- `PronunciationAttempt`
- `UserSettings`
- `ModelSettings`

Identifiers must be typed or validated and stable across content versions. Avoid using display strings as persistence keys.

The dependency-free core now includes validated identifiers, concept/progression/selection/transfer entities, `CafeOrderDefinition`, immutable task sessions/results, `TaskAttempt`, `ReviewHandoff`, separate speech-provider contracts, deterministic voice selection, transcript-based pronunciation assessment, bounded `PronunciationAttempt` metadata, deterministic `review-v1` scheduling, review-to-concept progression, and capability summaries. App-level scenario, pronunciation, and review controllers orchestrate pure engines/providers and the profile owner; Avalonia views render state and send intents. Broad task libraries, vocabulary progression, and retained-audio providers remain deferred.

## Suggested source layout

The exact .NET solution layout is selected during Milestone 1 after inspecting current Avalonia and .NET conventions. Start from this boundary map and simplify where the project does not need a separate project:

```text
src/
  Linguistics.App/
    Features/
    Persistence/
    Platform/
  Linguistics.Core/
    Models/
    Curriculum/
    Providers/
content/
tests/
  Linguistics.Core.Tests/
  Linguistics.App.Tests/
```

Do not create empty modules merely to match this diagram. Add a boundary when an implemented responsibility requires it.

## Primary runtime flow

```text
Learner profile + concept progress + due review + content version + configuration
  -> deterministic next-concept selection
  -> deterministic transfer routing
  -> deterministic lesson and task composition
  -> text or local speech input
  -> deterministic parsing where possible
  -> optional schema-constrained model classification
  -> deterministic task transition validation
  -> bounded local NPC response
  -> deterministic evaluation and feedback ranking
  -> persisted attempt, progress, and review schedule
```

Given identical learner state, content version, configuration, clock input, and seed, selection of the next concept, task type, bridge, and review items must be reproducible.

Given identical validated packs and course configuration, unit order, lesson order, slide IDs, and slide content must also be reproducible. Presentation projection may reuse reviewed pack text and fixed product copy. Model generated presentation proposals remain drafts until validated and accepted through the authoring boundary.

## Lesson-template rendering boundary

`Linguistics.Core.Content` owns typed template IDs, schemas, parameter values,
instances, reference resolution, and deterministic interaction evaluation. Pack loading
and validation fail closed before catalog projection. `CourseCatalogBuilder` preserves
authored template-instance IDs and order; a lesson without authored instances keeps the
existing deterministic generated-card fallback.

`Linguistics.App.Features.Learn.Templates` owns only presentation. Its registry maps an
application-known template ID to an Avalonia renderer. A renderer receives the shared
read-only content image cache, resolved parameters, one selected instruction language,
the effective reduced-motion setting, and an outcome callback. Renderers may play bounded choreography, expose replay and
skip, and present an authored text-only equivalent, but they do not receive a learner
repository, profile owner, mastery service, scheduler, or persistence handle. The
callback reports a local `Ready`, `Success`, `Uncertain`, or `Failure` presentation
outcome; the deterministic core evaluator remains the authority for that result.
The registry may additionally adapt the existing optional local speech-synthesis
provider into a TTS-capable renderer. That provider can play authored captions only; it
does not expose microphone input, persistence, curriculum, scoring, or state authority,
and provider or voice unavailability must leave the complete caption path intact.
Recognition renderers may keep local selection, card-reveal, assignment, drag/drop, and
choreography state. They submit stable authored IDs or complete assignment snapshots to
`TemplateInteractionEvaluator`; only that pure core evaluator maps the input to a bounded
outcome. Pointer and keyboard routes therefore share the same result mapping, while drag
events remain presentation input rather than a second scoring implementation.
Construction renderers follow the same boundary. Cloze and preposition stages submit one
stable selection; conjugation and case controls submit an authored key/value mapping;
negation submits the selected token and slot; ordered train cars and complements submit
their complete stable-ID sequence. `EvaluateMappedPair`, `EvaluateSelectionPair`, and
`EvaluateWordOrder` remain pure core functions. Accordion, prefix-split, and question-flip
presentations use acknowledgement outcomes only after the authored interaction completes.
Listening renderers likewise keep only local playback and selection state. They expose
the complete written prompt or transcript, use the optional installed-system-voice
adapter without microphone access, and submit stable authored choices, ordered IDs, or a
typed response to the core. Dictation tolerance is a deterministic normalization policy
inside `TemplateInteractionEvaluator`; speech availability, animation, and renderer
timing never influence its outcome.

Speaking renderers keep the same authority boundary. They may receive the existing local
synthesis, recognition, and pronunciation-assessment providers, but they retain no audio,
transcript, mastery, or learner-history state and cannot invent a score. Recognition is
available only after the learner's microphone preference, an explicit per-action
disclosure, and a configured local model all permit it. Typed wording remains a complete
microphone-free route and is never presented as pronunciation evidence. Authored response
IDs, intelligibility-only assessment outcomes, or bounded in-memory tap intervals go to
the deterministic core; raw transcripts and timing values never leave through the
template callback. Production without supported evidence stays explicitly unscored.

Reading and writing renderers also keep all document, field, note, selection, and tile
state transient. Synthetic forms never write their entered values; the core receives only
authored field IDs and determines completeness and equality. Note checks use authored
required phrases and return only matched criterion IDs after deterministic normalization.
Menu and schedule extraction submit stable authored answer IDs, while spelling submits a
complete ordered tile-ID sequence to the existing word-order evaluator. Renderers never
inspect learner history, persistence, mastery, or transfer routing. An unavailable sign
photograph becomes the complete authored sign rather than a fabricated or remote asset.

The developer gallery supplies fixed parameters backed by the validated pack and cycles presentation states
without reading learner history. Bundled machine-validated lesson instances are preview
only: finishing one returns to the course map without creating mastery evidence or a
progress record. Asset references remain optional; omission, text-only mode, or local
decode failure renders complete authored text instead of an invented or remote asset.

## Curriculum authoring flow

Canonical curriculum is never improvised at runtime:

```text
Research -> structured draft -> source attribution -> linguistic review
-> schema validation -> graph and reference tests -> version-controlled pack
-> runtime read-only loading
```

If an authoring model helps draft a pack, its output remains a candidate until reviewed and validated.

## State machines

### Concept progression

The allowed states are `locked`, `available`, `introduced`, `practicing`, `provisionallyMastered`, `reviewDue`, and `mastered`. Only the curriculum engine may transition them using explicit evidence and thresholds.

The Milestone 2 engine is pure: callers supply prerequisite readiness, an optional validated attempt, a clock, and `progression-v1`. Attempts cannot be applied while a concept still needs an availability or due-review refresh. The engine returns the previous and current progress plus a structured transition reason; it does not persist by itself.

### Task progression

Each task template defines named states, allowed transitions, state-specific allowed model intentions, and deterministic success conditions. A model may propose only an enumerated label supplied in its request. The evaluator accepts or rejects it.

### Provider availability

Ollama, STT, TTS voices, or microphone permission may be absent. Availability is explicit state, not an exception hidden by the UI. Deterministic and scripted learning remains available wherever its own dependencies are present.

A local capability snapshot may include CPU architecture, total memory, Ollama availability, installed model metadata, microphone permission, and installed target-language voices. Use it to explain measured low-resource, balanced, or higher-quality options; never upload it or turn an unmeasured heuristic into a compatibility claim.

## Persistence boundary

Persist learner data separately from immutable content packs. At minimum, preserve:

- Stable identifiers and content-pack version used for each attempt.
- Separate communicative, accuracy, fluency, pronunciation, and target-concept evidence.
- Review scheduling inputs and outputs.
- Which transfer bridge was shown.
- User explanation and reduced-motion preferences. The legacy recording-retention field is schema compatibility only, not a current preference.
- Migration version.

A content update may add or supersede definitions but must not rewrite learner history. Migrations require tests, rollback or recovery reasoning, and explicit approval if they can delete or reinterpret data.

Schema 5 keeps profile, curriculum history, task attempts, review handoffs, pronunciation metadata, and deterministic review schedules/outcomes in the existing atomic learner-data document. Schemas 1–4 are migrated in memory and remain byte-for-byte unchanged until the next successful save. Profile saves preserve all histories; combined learning-state saves require the matching active profile ID and update curriculum/task/pronunciation/review records together; deletion removes the one document plus its temporary sibling. Raw task dialogue, speech transcripts, audio, and prompts are not persisted. A database remains unnecessary for the current volume.

## Concurrency and cancellation

Use structured concurrency. Make shared mutable provider or persistence state actor-isolated only where it is actually shared. Long-running model, transcription, synthesis, and audio operations must support cancellation and leave deterministic task state consistent after cancellation.

Never allow a late provider response from an obsolete request to mutate the current lesson. Associate asynchronous work with a request or session identifier and validate it before applying results.

## Explainability and logging

Local debug logging uses categories such as curriculum, routing, task, ollama, speech, assessment, and persistence. Logs must exclude recordings, secrets, full learner utterances by default, and unnecessary personal data.

Developer mode should eventually answer:

- Why was this concept or lesson selected?
- Why was this bridge selected or rejected?
- Which evidence caused this error or transition?
- Which allowed identifiers and schema were sent to the model?
- Which transition was proposed, accepted, or rejected?
- Which content and configuration versions were active?

With `LINGUISTICS_DEVELOPER_MODE=1`, the Learn destination now answers the selection, routing, lesson-type, and configuration-version questions for explicitly synthetic fixture data. It performs no persistence and does not expose learner history or claim reviewed linguistic content.

## Architectural tests

As boundaries are implemented, add tests that prevent:

- Avalonia features from owning curriculum calculations.
- Concrete Ollama or speech types from leaking into curriculum APIs.
- Unvalidated model output from reaching persistence.
- Content identifiers from being accepted without pack validation.
- Learner history from being stored inside content-pack locations.
- Network or account dependencies from becoming mandatory for the core loop.

Treat these checks as an architecture ratchet. Fix violations rather than adding exemptions for convenience.
