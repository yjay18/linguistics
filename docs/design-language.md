# Paper design language and motion

Linguistics uses a calm local learning studio language expressed through tactile paper rather than game-economy visuals. Warm paper canvases, restrained grain, irregular edges, tinted contact shadows, tape, stamps, and cutout margins establish the identity. Deep forest navigation signals privacy and place; mint, amber, coral, and blue distinguish progress, attention, feedback, and evidence without becoming scores.

## Shared materials

All color, brush, grain, and shadow resources live in `App.axaml` with matched Light and Dark theme dictionaries. `PaperCard`, `CutoutFrame`, `PaperTape`, `PaperStamp`, and `TornEdge` are reusable controls in `src/Linguistics.App/Controls/`; feature views select their semantic classes instead of recreating materials. Shadows are theme-tinted and never pure black. The shell, onboarding, Today, Progress, and Settings use these shared materials.

`PaperStage` is the fixed scene container. Its z-order is backdrop, paper wash, supporting cast, ambient pieces, taped label, foreground silhouettes, subject, reaction burst, and verdict. Attached properties provide head, shoulder, waist, and foot anchors plus per-layer offsets and transforms. The developer-only sandbox, exposed by `LINGUISTICS_DEVELOPER_MODE=1`, composes generated raster PNG cutouts with alpha and stepped puppet movement; it does not use SVG or XAML vector shapes as scene artwork. Native paths remain appropriate for small UI decorations such as tape edges.

The sandbox images under `Assets/PaperStage/` are developer visual fixtures, not reviewed lesson-pack content. If promoted into a content pack, Phase 3 must add the required generated-asset provenance, hash, size-budget, and review records.

## Motion

Motion is presentation-only and never gates state. `SteppedEasing` supplies deterministic low-frame-rate puppet movement, while `PaperChoreography` sequences bounded stages, supports replay and synchronous skip-to-final behavior, and rejects timelines of four seconds or longer. Tape settles, stamps press, and the sandbox scene establishes, enters, reacts, and resolves in under four seconds.

Every choreography uses `MotionPreferences.ShouldReduce`. The saved “Reduce interface motion” preference and `LINGUISTICS_REDUCED_MOTION=1` both render the same complete final composition instantly while preserving color, focus, text, layout, and live-status cues. Existing 140 ms control feedback and 180 ms page transitions remain for ordinary non-puppet motion when reduction is off.

## Privacy and verification

The current speech adapter processes microphone audio transiently and never retains it. Onboarding and Settings therefore expose no retention preference; Settings keeps only the maintenance action for deleting any legacy or temporary audio files.

Automated tests parse both theme dictionaries, calculate contrast for every new semantic color pair, enforce paired resources and tinted shadows, verify stage order and anchors, and prove stepped frame counts, reduced-motion final state, sub-four-second timelines, and skip behavior. Visual checks and screenshots remain separate from keyboard, VoiceOver, Narrator, and real-interaction evidence.
