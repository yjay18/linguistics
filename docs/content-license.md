# Content licensing status

The first German and transfer packs are machine-validated internal review drafts. Their original wording is not licensed for public redistribution. Source records inside each pack identify the references used, their stated licenses, intended use, attribution, and unresolved review status.

Machine validation is not linguistic, pedagogical, or legal approval. Public redistribution remains blocked until a competent linguistic reviewer approves the learner-facing claims and an authorized reviewer confirms the pack and source-license obligations.

## Image assets

The German pack's `assets.json` is the canonical per-image record. It contains the
processed file hash and byte size, original-source hash, provenance, complete attribution,
license location, transformation record, QA notes, and pending review state. Settings and
each image-led lesson scene present these records locally.

The current Commons seed uses only files whose API metadata reported Public Domain,
CC0, CC BY, or CC BY-SA. That automated filter is conservative but is not legal review.
Each record remains `pending`; `modificationReviewed` and `redistributionReviewed` remain
false. Crops and background removals are marked as derivatives, and any derivative CC
BY-SA image must retain share-alike obligations. Public redistribution remains blocked
until a reviewer checks the Commons file history, attribution, license version, source
hash, transformation, and intended distribution.

The five paper-stage images are labelled `generatedIllustration` with generator name,
prompt summary, and original authoring-source hash. They are not photographs or evidence
of real people or places. `LicenseRef-Generated-Internal-Draft` grants no public
redistribution permission; generated outputs remain Preview until ownership, content,
and release review are recorded.

Full-resolution generated sources live only under `tools/AssetPipeline/Sources/generated/`.
The app bundles only processed files below `content/languages/de/assets/`, each below
300 KiB and covered by one manifest record. Each template instance may reference at most
300 KiB of distinct images in total, and the full pack image budget is 40 MiB.
