# Optional local speech

Speech is additive. The café task remains fully completable with text and captions when playback, a microphone, `whisper.cpp`, or a speech model is unavailable.

## Playback

Linguistics discovers installed voices through the operating system and filters them by the target language. macOS playback uses `/usr/bin/say`; Windows playback uses Windows PowerShell and `System.Speech.Synthesis.SpeechSynthesizer`. Learner text is sent through standard input, never interpolated into a shell command. Playback supports normal and slower rates, stop/cancellation, captions, deterministic voice choice for a fixed seed and voice inventory, and an explicit unavailable state. Generated audio is not saved.

## Microphone transcription

The first adapter invokes a separately installed `whisper-stream` process with fixed arguments, German as the task language, one explicitly configured model path, VAD mode, a 15-second limit, and audio saving omitted. The app accepts only the first completed transcription block, kills only its owned child process, rejects late request IDs, and leaves deterministic task state unchanged on denial, cancellation, silence, missing devices, missing models, or provider failure.

Set `LINGUISTICS_WHISPER_STREAM` to the exact executable when it is not on `PATH`, and set `LINGUISTICS_WHISPER_MODEL` to a model file you acquired and reviewed. The Settings check reports the configured model filename, local size, provider contract version, upstream source, and license caveat. Linguistics never downloads, bundles, or selects a model automatically.

The adapter does not pass `--save-audio`; therefore it creates no temporary or retained recording. The saved retention toggle remains a future-provider preference and is explicitly not acted on by this adapter. The independent deletion action still removes only app-owned `.wav` files should a later approved provider create them.

## Pronunciation evidence

The current assessment compares normalized expected words with the local recognizer transcript using an ordered word match. It reports a categorical outcome, expected/recognized/matched word counts, and missing or unexpected words. It does not produce phoneme scores, articulator diagnoses, confidence values, accent percentages, or native-likeness ratings.

The transcript is visible long enough to review but is not persisted. Schema 5 stores only the utterance ID, time, duration, categorical outcome, counts, transcript-match ratio, and content/provider/assessment versions. Editing a café transcript before sending converts it to text input and removes pronunciation evidence.

## Current distribution boundary

The app does not redistribute `whisper.cpp`, a speech model, or a tested Windows microphone configuration. On the development Mac, `whisper.cpp` 1.9.1 and system German voices were discovered, but no usable German Whisper model was configured for the app. Real microphone transcription, Windows playback/capture, Narrator, model performance, and redistribution remain release evidence rather than inferred claims.
