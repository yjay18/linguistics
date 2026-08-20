# Learner-data storage

Linguistics stores one schema-versioned JSON learner-data document in the current user's application-data directory:

- macOS: `~/Library/Application Support/com.yjay18.linguistics/learner-profile.json`
- Windows: `%LOCALAPPDATA%\com.yjay18.linguistics\learner-profile.json`

`LINGUISTICS_DATA_DIRECTORY` may override the directory for isolated development and automated testing. Production code does not store learner data in the application bundle or content directories.

The current envelope schema is version 5. It contains the learner profile, the optional identifier of an explicitly selected local Ollama model, concept progress, concept attempts, task attempts, review handoffs, pronunciation-attempt metadata, deterministic review schedules/outcomes, separate evidence dimensions, active configuration/content/evaluation/assessment/review versions, dialogue realization mode, and the mapping ID/version/routing version/score for any selected transfer bridge. Pronunciation metadata is limited to an utterance ID, time, duration, categorical outcome, expected/recognized/matched word counts, a transcript-match ratio, and provider/content versions. Review metadata is limited to stable item/target IDs, kind, content/configuration versions, last-seen and due times, success streak, failure count, difficulty, recent response latency, and an enumerated rating used by `review-v1`. It does not store café conversation text, speech transcripts, audio, or model prompt bodies. Writes go to `learner-profile.json.tmp` and move into place only after serialization and flushing succeed. Curriculum, task, pronunciation, and review history are saved together. An unsupported schema or corrupt document fails with an attributable error and is not silently rewritten or deleted.

Schema 1 profile-only, schema 2 profile-plus-curriculum, schema 3 task-history, and schema 4 pronunciation-history files remain readable. Loading any of them does not rewrite it. The next successful save writes schema 5, preserving existing data and adding empty task, pronunciation, or review history where required; migration failures leave the original file unchanged.

Deleting all learning data removes only `learner-profile.json` and its temporary sibling, including the profile, curriculum history, task attempts, review handoffs, pronunciation metadata, and review schedule inside that document. It also invokes the separately scoped speech-recording deletion path before deleting the profile. “Delete speech recordings” targets only `.wav` files below the app-owned `Speech Recordings` directory, skips filesystem links, and preserves learning history and unrelated files. The current `whisper-stream` adapter never enables audio saving, so this directory is normally empty. Models and content packs are separate data classes.

The profile owner serializes restore, create, update, learning-state save, and delete intents. Learning persistence requires the matching active profile identifier. After deletion, neither a stale profile update nor a late learning-state save can recreate learner data.
