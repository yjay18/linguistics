# Learner-profile storage

Milestone 1 stores one schema-versioned JSON learner profile in the current user's application-data directory:

- macOS: `~/Library/Application Support/com.yjay18.linguistics/learner-profile.json`
- Windows: `%LOCALAPPDATA%\com.yjay18.linguistics\learner-profile.json`

`LINGUISTICS_DATA_DIRECTORY` may override the directory for isolated development and automated testing. Production code does not store learner data in the application bundle or content directories.

The current envelope schema is version 1. Writes go to `learner-profile.json.tmp` and move into place only after serialization and flushing succeed. An unsupported schema or corrupt document fails with an attributable error and is not silently rewritten or deleted.

At this stage, deleting all learning data removes only `learner-profile.json` and its temporary sibling. It does not recursively delete the containing directory or touch unrelated files. Models and future content packs are separate data classes.
