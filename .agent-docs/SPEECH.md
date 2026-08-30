# Speech and Pronunciation

## Scope

Speech is a local, replaceable capability with three distinct boundaries:

- `SpeechSynthesisProvider` turns approved text into audio.
- `SpeechRecognitionProvider` turns captured audio into a transcript and available timing metadata.
- `PronunciationAssessmentProvider` turns audio plus an expected utterance into explicitly supported evidence.

Do not collapse these boundaries. Transcription success is not a phoneme score.

## Current implementation

As rechecked and implemented on 2026-08-20:

- The first no-dependency system TTS adapter invokes `/usr/bin/say` on macOS and Windows PowerShell with `System.Speech.Synthesis.SpeechSynthesizer` on Windows. Text goes through standard input and every option uses `ProcessStartInfo.ArgumentList`; no learner string is interpolated into a shell command. Apple and Microsoft system speech documentation remains the platform reference: <https://developer.apple.com/documentation/avfaudio/speech-synthesis> and <https://learn.microsoft.com/dotnet/api/system.speech.synthesis.speechsynthesizer>.
- The first STT adapter invokes an explicitly installed `whisper-stream` process with VAD, German language, a 15-second bound, a configured model path, no fallback decoding, and audio saving omitted. Upstream calls the stream example a proof of concept, so it remains optional and replaceable: <https://github.com/ggml-org/whisper.cpp/tree/master/examples/stream>.
- `whisper.cpp` 1.9.1 is installed on the development Mac under Homebrew and is MIT-licensed. No usable German model is configured, no model was downloaded, and the binary/model are not redistributed. OpenAI's Whisper model card warns about hallucinations and uneven language/accent performance, so recognition is never treated as a phoneme or accent judge: <https://github.com/openai/whisper/blob/main/model-card.md>.

The adapters are installed code paths, not bundled dependencies or a release-support claim. Windows interaction, real German recognition, model accuracy/performance, minimum OS behavior, binary/model redistribution, and signing remain release evidence.

The current stream adapter never passes `--save-audio`. Capture therefore creates no temporary or retained recording. The learner interface does not offer recording retention; the schema field remains only for backward-compatible reads and is written false by current onboarding and Settings flows. “Delete speech recordings” remains available for exact-scope cleanup and deletes only `.wav` files below the app-owned directory while skipping filesystem links.

`TranscriptPronunciationAssessmentProvider` normalizes words, computes an ordered match, and returns only a categorical outcome, word counts, missing/unexpected words, duration, and version. The visible transcript is not persisted. Schema 4 stores only bounded evidence metadata. Editing a café transcript switches the attempt to text input and removes pronunciation evidence.

Do not make Piper or any other single neural TTS implementation a core curriculum dependency. A future local neural voice engine must remain an optional provider and pass a fresh maintenance, license, privacy, performance, and distribution review.

## End-to-end speech flow

```text
microphone permission
-> bounded recording session
-> local audio normalization
-> local transcription with target language where appropriate
-> transcript and supported timing metadata
-> deterministic evaluation plus optional constrained interpretation
-> deterministic task transition
-> approved NPC text
-> local speech synthesis
```

Text input and captions remain available at every point. Speech-provider failure must not corrupt task state.

## Speech synthesis

The first system adapter should:

- Enumerate installed voices appropriate to the target language.
- Expose stable local identifiers without assuming a particular voice exists.
- Speak, pause, resume, stop, and cancel.
- Apply bounded user-controlled speech rate.
- Report unavailable voice and synthesis failures explicitly.
- Do not retain generated or microphone audio. Delete any provider-created temporary audio file after use.
- Keep synthesis details outside curriculum and Avalonia views.

### High-variability perception

Perception activities may specify an utterance, target phoneme or contrast, phonological context, and a voice policy. When multiple suitable voices are installed, choose among them using a seed derived from lesson or session identity. The same seed and voice inventory must produce the same choice.

An activity may request a `minimumDistinctVoices` value, such as three, but that is a training preference rather than a launch precondition.

If too few appropriate voices exist, use the available set and state the limitation in developer diagnostics. Never relabel one voice or pitch shift as several independent speakers.

## Speech recognition

The first local adapter is expected to use `whisper.cpp`, subject to the dependency and license review. Its domain result should contain only metadata the integration actually supports, such as:

- Transcript.
- Language information when returned or fixed by request.
- Segment or token timestamps when reliably available.
- Duration.
- Provider diagnostics safe for local developer mode.

The adapter owns audio-format conversion, model loading, request cancellation, and upstream errors. The curriculum sees a domain result, not C/C++ structures or a process invocation.

Do not silently download a speech model. Show model identity, size, source, license status, storage location, expected capability, and explicit download consent.

## Audio capture

- Request microphone permission only when the learner chooses a speech action or opts in during onboarding.
- Explain that text-only use remains available after denial.
- Keep the capture indicator visible.
- End capture promptly when the learner stops, exits, or the request is cancelled.
- Associate audio with a task and request ID so late callbacks cannot affect a newer attempt.
- Normalize locally into the format expected by the provider.
- If a provider creates a temporary audio file, delete it after the result. Audio retention is not offered or planned by the current product policy.
- Handle interruption, missing device, permission denial, silence, excessive duration, and malformed audio.

## Pronunciation assessment

The first implementation may honestly report:

- Whether the expected phrase was intelligible to the local recognizer under the tested conditions.
- Expected phrase versus recognized phrase.
- Word-level mismatches.
- Attempt duration.
- Hesitation or segment timing only where the provider supplies reliable evidence.
- Repeated failures across comparable attempts.

It must not fabricate:

- Phoneme-level scores.
- Articulator or tongue-position diagnoses.
- Accent percentages.
- Native-likeness ratings.
- Confidence values that the provider did not return.

A future local assessment model may return word and phoneme evidence behind the same protocol. Its output requires calibration, validation, provenance, license review, and learner-facing explanation before use.

## Feedback priority

Pronunciation feedback prioritizes word identity, substantial intelligibility loss, comprehension-affecting stress, and comprehension-affecting timing before optional refinement. The goal is effective communication, not accent eradication.

## Privacy

Speech never needs to leave the Mac. The current product does not offer recording retention and does not save microphone audio. “Delete speech recordings” must work independently of “Delete all learning data” so any app-owned legacy files can still be removed.

Do not include audio, full transcripts, or learner-identifying paths in ordinary logs. See `PRIVACY.md`.

## Accessibility and recovery

Every speech activity provides captions, replay, adjustable rate, keyboard-accessible controls, clear focus order, and a text-only path. Distinguish unavailable provider, missing model, missing voice, permission denial, silence, transcription failure, and cancellation.

The learner must still be able to review vocabulary, use transfer notes, perform scripted tasks, and access non-speech lessons when STT is unavailable.

## Required tests and evidence

- Voice selection is deterministic for fixed seed and inventory.
- Missing or insufficient voices degrade without crashing.
- Stop and cancellation terminate synthesis and capture.
- Permission denial preserves text-only use.
- Any provider-created temporary audio is deleted after success, failure, and cancellation.
- Legacy audio-file cleanup removes only the intended app-owned files.
- A stale transcription cannot mutate the current task.
- STT failure leaves deterministic state unchanged and offers recovery.
- Unsupported assessment fields remain absent rather than synthesized.
- Real microphone, playback, captions, rate, keyboard, Narrator, and VoiceOver flows are verified on supported hardware for the claimed platforms.
- Dependency artifacts, model files, and redistribution terms are audited before release.
