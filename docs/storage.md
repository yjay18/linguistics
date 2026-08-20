# Learner-data storage

Linguistics stores one schema-versioned JSON learner-data document in the current user's application-data directory:

- macOS: `~/Library/Application Support/com.yjay18.linguistics/learner-profile.json`
- Windows: `%LOCALAPPDATA%\com.yjay18.linguistics\learner-profile.json`

`LINGUISTICS_DATA_DIRECTORY` may override the directory for isolated development and automated testing. Production code does not store learner data in the application bundle or content directories.

The current envelope schema is version 3. It contains the learner profile, the optional identifier of an explicitly selected local Ollama model, concept progress, concept attempts, task attempts, review handoffs, separate evidence dimensions, active configuration/content/evaluation versions, dialogue realization mode, and the mapping ID/version/routing version/score for any selected transfer bridge. It does not store café conversation text or model prompt bodies. Writes go to `learner-profile.json.tmp` and move into place only after serialization and flushing succeed. Curriculum and task history are saved together, so one task cannot leave them out of sync. An unsupported schema or corrupt document fails with an attributable error and is not silently rewritten or deleted.

Schema 1 profile-only and schema 2 profile-plus-curriculum files remain readable. Loading either does not rewrite it. The next successful save writes schema 3, preserving existing data and adding empty task history where required; migration failures leave the original file unchanged.

Deleting all learning data removes only `learner-profile.json` and its temporary sibling, including the profile, curriculum history, task attempts, and review handoffs inside that document. It does not recursively delete the containing directory or touch unrelated files. Models and content packs are separate data classes.

The profile owner serializes restore, create, update, learning-state save, and delete intents. Learning persistence requires the matching active profile identifier. After deletion, neither a stale profile update nor a late learning-state save can recreate learner data.
