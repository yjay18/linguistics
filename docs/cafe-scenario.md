# Café scenario

The first complete task is a text-and-local-speech café exchange. Its narrow goal is to order one coffee with the reviewed request frame once learner-facing content becomes runtime-eligible.

## Authority and fallback

`CafeOrderEngine` is a pure deterministic reducer. It owns the waiting, request-frame, and complete states; recognizes the required `ich möchte` frame and `Kaffee`; checks `einen Kaffee` and noun capitalization; ranks one primary correction; retains context for retry; and creates separate communicative, accuracy, fluency, and target-concept evidence. The same reducer accepts typed text or an unchanged local transcript. Editing a transcript treats it as text. Only the unchanged speech path may attach a clearly labelled word-recognition intelligibility proxy.

The optional local model receives only the deterministic turn result and may choose an exact supplied server line. Scripted responses complete every state without Ollama. A timeout, cancellation, unavailable service, invalid proposal, or absent model cannot weaken the task contract.

## Learning history

On success, schema 4 atomically stores a concept attempt, task attempt, target-concept progression decision, and review handoff. The record includes content/evaluation/schema versions, input mode, bounded speech evidence when present, model realization mode, encountered error-rule IDs, and any confirmed multilingual bridge reference. It excludes raw learner and server messages, transcripts, and audio.

System playback is optional and the visible NPC caption remains authoritative. Missing providers, microphone denial, cancellation, silence, model failure, or a stale transcript cannot advance the task; typing remains available.

If the atomic save fails, the completed in-memory record is retained for an explicit retry rather than silently reporting success or creating a duplicate attempt.

## Content gate

The repository's bundled German, English-transfer, and Hindi-transfer packs remain machine-validated drafts. Normal runtime loading therefore fails closed and the Scenarios screen explains that teaching is paused. Automated and native QA may use a temporary copy marked by a synthetic test reviewer; that fixture proves the code path and is not linguistic or license approval.
