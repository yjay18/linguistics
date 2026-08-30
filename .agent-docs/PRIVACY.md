# Privacy and Local Data

## Default promise

The app requires no account, remote backend, telemetry, or cloud inference. Learning history and speech processing remain local. Microphone audio is processed transiently on-device and is never retained or stored. If a provider creates an app-owned temporary audio file, it is deleted after the request.

“Local first” does not mean that initial downloads are impossible: the app, optional Ollama models, optional speech models, and content updates may require a network to obtain. Downloads must be explicit and described; completed core learning should not require an ongoing connection.

## Data inventory

Expected learner data includes:

- Learner profile and known-language preferences.
- Target language.
- Concept progress and attempts.
- Task attempts and error history.
- Review schedule and vocabulary state.
- Pronunciation attempt metadata.
- User, model, voice, and accessibility settings. The legacy retention field remains schema-compatible but current flows write it false.

Content packs, model files, app logs, provider-created temporary audio files, and generated caches are separate data classes with separate ownership and deletion behavior. Temporary audio files are never learner recordings and are deleted after the request.

## Collection minimization

- Collect only fields required by an approved behavior.
- Do not request a name, email address, account, demographic profile, or precise location for the MVP.
- Do not add analytics SDKs or persistent identifiers.
- Do not infer ethnicity, nationality, or identity from language repertoire or speech.
- Do not use recordings or learning history to train a model.
- Do not retain full prompts, transcripts, or audio in ordinary logs.

## Storage

Choose documented per-user application-data locations for macOS and Windows during Milestone 1. Store structured learner data through the repository boundary. If a provider requires a temporary audio file, keep it in an app-owned temporary location and delete it after success, failure, or cancellation.

Define and test:

- File and database locations.
- Backup behavior, including confirmation that microphone audio is never retained and temporary audio files are removed before backup can include them.
- File-protection and permission behavior available on supported macOS and Windows versions.
- Migration and corruption recovery.
- Content/model storage separately from user history.
- Cleanup of temporary files after success, failure, and cancellation.

Never store learner data inside the app bundle or a content-pack directory.

The app stores one bounded app-owned JSON document rather than adding a database. Schema 6 may contain concept progress and attempts, task attempts, review handoffs, bounded pronunciation metadata, deterministic review schedules and outcomes, lesson visit position and counts, separate evidence dimensions, configuration and content versions, dialogue realization mode, and selected bridge references beside the profile. Lesson visits contain only lesson and concept IDs, counts, card position, times, and content version. They do not create mastery evidence. Pronunciation metadata is limited to utterance ID, time, duration, categorical outcome, expected, recognized, and matched word counts, a transcript match ratio, and provider and content versions. Review metadata contains stable IDs, type, due and last seen time, streak, failures, difficulty, recent response latency, content and configuration versions, and an enumerated rating. The store does not contain raw café dialogue, speech transcripts, audio, or prompt bodies. Schema 1 through 5 migrations are read only until a successful save; failed or unsupported input is not overwritten. Combined learning state saves require the matching active profile ID, and exact file deletion removes profile and all histories without recursively touching the directory.

## Microphone and speech

Request microphone access at the point of use or through an explicit onboarding choice. Explain the purpose and provide a fully usable text-only path after denial.

Default speech lifecycle:

1. Start a visible local speech capture.
2. Normalize and process audio transiently on-device.
3. Produce supported evidence.
4. Delete any provider-created temporary audio file.
5. Store only the minimum attempt metadata needed for learning.

The first `whisper-stream` adapter omits its audio-save flag, so it creates no temporary or retained recording. It requests microphone access only after the learner clicks a speech action and confirms a visible local-processing disclosure. A 15-second bound, visible capture indicator, cancellation, request ID, and owned-process termination protect the session. Missing model, denial, missing device, silence, failure, and cancellation preserve the complete text path.

The learner interface does not expose recording retention. The schema field remains for backward-compatible reads, but current onboarding and Settings flows write it false. Independent legacy-audio cleanup targets only `.wav` files beneath the app-owned `Speech Recordings` directory and skips filesystem links. Delete-all invokes this scoped cleanup before removing the learner document.

## Local model boundary

The default LLM adapter uses only an explicitly local endpoint. The app must not silently select an Ollama cloud model or transmit a prompt to a remote host. Prompt construction includes only the current bounded task context.

Model and speech-model downloads are external network actions. Display source, file size, license status, storage impact, and whether the artifact will execute locally before user approval.

## Logging and developer mode

Production logs may contain categories, timestamps, request IDs, durations, non-sensitive configuration versions, and error codes. They must exclude by default:

- Full learner utterances.
- Audio.
- Prompt bodies.
- Database rows.
- Personal file paths.
- Secrets or tokens.

Developer mode may reveal a current prompt for explainability only through an intentional local action and must warn that learner text may be present. Exporting logs or diagnostics is a separate user-authorized action with a preview and redaction step.

The Milestone 2 diagnostic uses fixed synthetic concept and mapping IDs plus aggregate scores. It does not read persisted attempts, display learner utterances, write logs, mutate progress, or make a network request.

## Deletion

Provide two distinct actions:

### Delete speech recordings

Deletes app-owned legacy or temporary audio files while preserving learning history. Current microphone capture does not create retained recordings, so this action normally finds nothing. It reports completion or the exact files it could not remove without exposing unrelated paths.

### Delete all learning data

Deletes learner profiles, attempts, schedules, settings considered personal, any app-owned legacy or temporary audio files, redacted diagnostic logs, app-owned recovery copies, and app-owned derived caches. Bundled app resources may remain. Downloaded models and content packs should be handled explicitly because they may be large but are not learner history.

Both actions require clear scope, confirmation proportional to irreversibility, transactional or recoverable design where practical, and verification. Never claim deletion based only on dismissing a view.

## Telemetry and research

The MVP contains no analytics or crash-reporting SDK that sends data remotely. The implemented local JSON-lines diagnostic log is bounded to 256 KiB and accepts only timestamp, fixed category/event/outcome enums, optional request ID, bounded duration, and a validated configuration-version token. Its API has no free-form message, path, payload, learner text, transcript, audio, prompt, or response field. Any future telemetry, sync, research export, or shared curriculum evaluation requires a separate product decision covering purpose, minimization, consent, revocation, retention, recipients, security, and deletion.

Consent to learn a language is not consent to research use.

## Threat and failure boundaries

Plan and test for:

- A provider accidentally configured to a remote endpoint.
- Logs containing learner content.
- Temporary audio left after a crash or cancellation.
- Late async callbacks writing to a deleted profile.
- A migration exposing or losing history.
- Pack updates rewriting learner evidence.
- Path traversal or overbroad deletion.
- Imported content containing malformed or hostile data.
- Model output attempting to introduce unknown identifiers or instructions.

Treat content and model output as untrusted input. Restrict deletion to resolved, app-owned paths and never use broad home-directory targets.

## Privacy acceptance evidence

Before an affected feature is accepted, provide evidence for:

- No mandatory account or remote request in the core flow.
- Local endpoint enforcement.
- Permission-denied behavior.
- Any provider-created temporary audio deletion after success, failure, and cancellation.
- Separate legacy-audio cleanup and all-data deletion.
- Database and file migration behavior.
- Log redaction.
- No secrets or personal payloads in fixtures.
- Accessibility of privacy controls.
- Clear network disclosure for downloads.
- Manual inspection of app-owned storage before and after deletion.

Security and privacy review is mandatory for changes to persistence, microphone use, recordings, model endpoints, imports, exports, diagnostics, deletion, or distribution.
