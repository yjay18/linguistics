# Learner-data storage

Linguistics stores one schema-versioned JSON learner-data document in the current user's application-data directory:

- macOS: `~/Library/Application Support/com.yjay18.linguistics/learner-profile.json`
- Windows: `%LOCALAPPDATA%\com.yjay18.linguistics\learner-profile.json`

`LINGUISTICS_DATA_DIRECTORY` may override the directory for isolated development and automated testing. Production code does not store learner data in the application bundle or content directories.

The current envelope schema is version 2. It contains the learner profile plus concept progress, concept attempts, separate evidence dimensions, active progression and selection configuration versions, and the mapping ID/version, routing-configuration version, and score for any selected transfer bridge. Writes go to `learner-profile.json.tmp` and move into place only after serialization and flushing succeed. An unsupported schema or corrupt document fails with an attributable error and is not silently rewritten or deleted.

Schema 1 profile-only files remain readable. Loading one does not rewrite it. The next successful profile or curriculum save writes schema 2 with the original profile and an empty curriculum history; migration failures leave the schema 1 file unchanged.

Deleting all learning data removes only `learner-profile.json` and its temporary sibling, including the profile and curriculum history inside that document. It does not recursively delete the containing directory or touch unrelated files. Models and future content packs are separate data classes.

The profile owner serializes restore, create, update, and delete intents. Curriculum persistence requires the matching active profile identifier. After deletion, neither a stale profile update nor a late curriculum save can recreate learner data.
