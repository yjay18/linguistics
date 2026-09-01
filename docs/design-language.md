# Paper design language and motion

Linguistics uses a calm local learning studio language expressed through tactile paper rather than game-economy visuals. Warm paper canvases, restrained grain, irregular edges, tinted contact shadows, tape, stamps, and cutout margins establish the identity. Deep forest navigation signals privacy and place; mint, amber, coral, and blue distinguish progress, attention, feedback, and evidence without becoming scores.

## Shared materials

All color, brush, grain, and shadow resources live in `App.axaml` with matched Light and Dark theme dictionaries. `PaperCard`, `CutoutFrame`, `PaperTape`, `PaperStamp`, and `TornEdge` are reusable controls in `src/Linguistics.App/Controls/`; feature views select their semantic classes instead of recreating materials. Shadows are theme-tinted and never pure black. The shell, onboarding, Today, Progress, and Settings use these shared materials.

`PaperStage` is the fixed scene container. Its z-order is backdrop, paper wash, supporting cast, ambient pieces, taped label, foreground silhouettes, subject, reaction burst, and verdict. Attached properties provide head, shoulder, waist, and foot anchors plus per-layer offsets and transforms. The developer-only sandbox, exposed by `LINGUISTICS_DEVELOPER_MODE=1`, composes generated raster PNG cutouts with alpha and stepped puppet movement; it does not use SVG or XAML vector shapes as scene artwork. Native paths remain appropriate for small UI decorations such as tape edges.

The sandbox now resolves five generated scene assets from the validated German content pack through the same version-keyed local image cache as lesson templates. Their generated provenance, original/final hashes, prompt summaries, transformations, size budgets, and Preview review state live in `content/languages/de/assets.json`; authoring originals remain outside the shipped app under `tools/AssetPipeline/Sources/generated/`.

The Wave A presentation catalog reuses this same vocabulary for establishing scenes,
object labels, captioned dialogue, short routes, postcards, albums, cultural plates,
weather windows, and clocks. Missing optional imagery produces an authored paper-text
composition, never a placeholder box or network request. Optional caption playback sits
in a soft paper control strip, keeps the complete caption visible, and uses no
microphone. Template-specific styling may arrange the shared materials, but it does not
introduce another accent family.

The Wave B recognition catalog keeps the same stage, paper card, cutout, tape, and stamp
vocabulary for picture and word choices, matching pairs, category sorting, articles,
plural folds, color chips, quantities, and scene hotspots. Shared choice layouts keep
labels visible in both themes and at reduced motion. Sortable cutouts retain a complete
select-then-assign keyboard route, and every image-led composition carries its authored
text-only equivalent.

The Wave C construction catalog treats grammar as physical arrangement without adding
another accent family. Reserved train cars, cloze tiles, accordion folds, paired wheels,
switches, detachable prefixes, flip cards, labeled sentence slots, position targets, and
complement cutouts all use the shared paper materials. Controls keep their authored
labels visible in both themes, selected arrangements are repeated in polite live text,
and every drag-led scene also has a select-then-place keyboard route. Wrong arrangements
may wobble as presentation feedback, but stable authored IDs still go to the deterministic
core evaluator before an outcome is shown.

The Wave D listening catalog presents complete authored transcripts alongside local
playback controls. Image choices, event strips, typewriter cards, sound doors, route
stops, price tags, and dialogue cutouts keep the same restrained paper vocabulary and
one accent family. Selected items use explicit accent ink in both themes, and scenic
labels remain clear of tape and dialogue layers. Playback is optional presentation;
stable authored responses still go to the deterministic core evaluator.

## Motion

Motion is presentation-only and never gates state. `SteppedEasing` supplies deterministic low-frame-rate puppet movement, while `PaperChoreography` sequences bounded stages, supports replay and synchronous skip-to-final behavior, and rejects timelines of four seconds or longer. Tape settles, stamps press, and the sandbox scene establishes, enters, reacts, and resolves in under four seconds.

Every choreography uses `MotionPreferences.ShouldReduce`. The saved “Reduce interface motion” preference and `LINGUISTICS_REDUCED_MOTION=1` both render the same complete final composition instantly while preserving color, focus, text, layout, and live-status cues. Existing 140 ms control feedback and 180 ms page transitions remain for ordinary non-puppet motion when reduction is off.

## Privacy and verification

The current speech adapter processes microphone audio transiently and never retains it. Onboarding and Settings therefore expose no retention preference; Settings keeps only the maintenance action for deleting any legacy or temporary audio files.

Automated tests parse both theme dictionaries, calculate contrast for every new semantic color pair, enforce paired resources and tinted shadows, verify stage order and anchors, and prove stepped frame counts, reduced-motion final state, sub-four-second timelines, and skip behavior. Visual checks and screenshots remain separate from keyboard, VoiceOver, Narrator, and real-interaction evidence.
