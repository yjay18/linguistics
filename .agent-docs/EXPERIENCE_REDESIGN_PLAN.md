# Paper Theatre Experience Plan

Production redesign of the learner-facing experience: a paper-theatre visual identity, a
deterministic lesson-template engine with 58 templates, an image asset pipeline built on
openly licensed sources, and many-to-many language support so any repertoire (for example
English + Hindi) can learn any packed target language (first German) with instruction in
the language that suits the learner best.

This document is the canonical work list for long agentic workflows. Work items carry
stable IDs (`P2.3`, `P4.T21`) so any agent can pick one up, execute it as a vertical
slice, and leave the repository compiling with evidence.

## Inspection verdict (2026-08-30)

The codebase was inspected end to end before this plan was written.

**Keep unchanged.** The deterministic core is genuinely strong and must not be rebuilt:
typed identifiers, the concept graph and progression engine, deterministic selection with
injected clock/seed, transfer routing, the review scheduler, schema-5 atomic persistence,
the content-pack validator with provenance and license gates, the bounded Ollama adapter,
and the speech provider boundaries. All 171+ tests, the architecture ratchet, and every
invariant in `AGENTS.md` remain binding.

**Redesign.** The presentation layer is the weak half of the product. Today every lesson
is five near-identical text slides auto-generated from concept metadata
(`CourseCatalogBuilder.CreateLesson`), rendered as C#-built controls in
`LearnView.axaml.cs` with one crossfade. There are no images, no scene, no variety, and
no per-lesson authored presentation. The design tokens ("warm paper studio") are a good
foundation but stop at flat cards. This plan replaces slide generation with a template
engine, replaces the flat card look with a layered paper-theatre identity, and makes
lesson presentation authored, versioned pack data instead of code-side string assembly.

**Explicitly not a rewrite.** Avalonia stays (product principle 10 forbids a web
wrapper). No WebView, no new UI framework, no new animation dependency: Avalonia 12.1
keyframe animations, transitions, render transforms, and custom easings cover the whole
motion grammar in this plan.

## Design direction

Design read: local-first learning studio for multilingual adults, with a handcrafted
paper-theatre language: layered cutouts, taped labels, stamped badges, stepped puppet
motion. Calm and tactile, never a game economy. Dials: variance 7, motion 6, density 3.

### Paper materiality

Extend the existing token system (`App.axaml`) into physical materials, each a reusable
styled control or style class, both themes, contrast-tested like the current tokens:

- **Paper card** — existing card plus subtle grain, slightly irregular 1px edge, tinted
  shadow (never pure black). Grain is one shared tiled brush, drawn once, not per-card
  bitmap effects.
- **Cutout** — an image with a thin white "scissor margin" border and a soft contact
  shadow, optionally rotated 1–3 degrees. The core presentation unit for every image.
- **Tape** — small translucent parallelogram anchoring labels and notes; taped things
  enter with a press-down settle.
- **Stamp** — inked circular/rectangular seal for verdicts, articles (der/die/das), and
  warnings; enters with a single overshoot press.
- **Torn edge** — irregular polygon clip for section breaks and foreground silhouettes.
- **Paper stage** — the layered scene container (see template engine) with fixed layer
  order: backdrop, paper wash, supporting cast, ambient pieces, taped label, foreground
  silhouettes, player/subject, reaction burst, verdict card.

### Motion grammar

All motion is presentation only; it never gates state, and every choreography has a
reduced-motion equivalent that shows the same final composition instantly
(`MotionPreferences.ShouldReduce` already exists and stays the single gate).

- **Stepped puppet motion**: 6–8 fps stepped keyframes (custom stepped easing) for
  cutout charm — torso bob, opposing limb swings, settle wobbles. Never smooth-tween a
  puppet.
- **Smooth stage motion**: 180–260 ms eased transforms for cards, slides, and camera
  moves; the existing 140/180 ms interaction timings stay.
- **Choreography, not simultaneity**: scenes stage in sequence (establish backdrop →
  enter subject → land label → reveal consequence); every sequence is skippable with one
  action and finishes in under 4 seconds.
- **One reaction per outcome**: success hop, uncertain wobble, failure slump — expressed
  by the scene, decided by the deterministic evaluator. The animation never invents a
  different judgment.

### Taste constraints (adapted for a desktop app)

- One accent family (the existing forest/mint), amber for attention, coral for danger;
  no new accents, no purple, no glows.
- Max one eyebrow label per screen region; no section-number labels, no decorative
  status dots, no em-dashes in any user-facing string.
- Typography hierarchy by weight and size, display max two lines; body copy ≤ 25 words
  per block in lesson scenes.
- Real images from the asset pipeline, never placeholder rectangles or fake screenshots;
  a template that lacks its asset renders its authored text-only equivalent, not an
  empty frame.
- Empty, loading, error, unavailable, and recovery states designed for every new
  surface, matching final layout shapes.

## Architecture additions

Three new seams, all obeying the existing boundary map (`ARCHITECTURE.md`):

1. **Template engine** (`Linguistics.Core` contracts + `Linguistics.App` renderers).
   A `LessonTemplate` is a versioned, typed parameter schema; a pack authors *template
   instances* (template ID + parameters referencing concept/example/asset/task IDs); the
   app holds a registry mapping template IDs to Avalonia renderers. Slide generation in
   `CourseCatalogBuilder` becomes the fallback for lessons without authored instances,
   so the catalog never breaks while content catches up. Deterministic rule: identical
   packs + configuration ⇒ identical template instances, order, and IDs.
2. **Asset system** (pack data + validator + app loader). Packs gain an `assets/` folder
   and `assets.json` manifest; every asset carries license, author, source URL,
   retrieval date, and derivative status. The validator fails any referenced-but-missing
   asset, any asset without a complete license record, and any oversized file. Runtime
   loads assets read-only from validated packs; the app never fetches images from the
   network.
3. **Instruction-language routing** (`Linguistics.Core`). Learner-facing explanation
   strings become per-language maps; a pure `InstructionLanguageSelector` picks the
   teaching language from the learner's explanation consent, preferred language, and
   reading comfort, with a deterministic fallback chain. Target-language content stays
   language-pure; transfer packs stay per source-target pair, so languages scale
   many-to-many without combinatorial packs.

## Phases

Each phase is a sequence of agentic work packages. Rules for every work item:

- Load the `paper-animate` skill before building any scene/stage/choreography item, and
  keep its layering, alignment, reduced-motion, and verification rules.
- One vertical slice per item; the repo compiles and all tests pass at every stop point.
- Evidence per repo convention: build + tests + formatter, plus real interaction
  (screenshots alone are visual evidence only) for UI items.
- New learner-visible content follows the review gates: machine-validated drafts stay in
  Preview; nothing is described as approved without competent review.
- End each item in one truthful state (complete and verified / complete with named
  unverified evidence / blocked / paused / proposal).

### Phase 0 — Decisions and scaffolding

- **P0.1** Record binding decisions in this document once made: raster PNG cutouts with
  alpha as the asset format (paper decorations drawn natively, so no SVG dependency);
  system font stack retained unless a bundled open font is separately approved;
  per-template asset budget 300 KB, per-pack budget 40 MB.
- **P0.2** Verify Devanagari rendering quality in Avalonia 12.1 on macOS (and note
  Windows as named unverified if unavailable): Hindi strings in cards, labels, and
  automation names. Blocking issues become a named dependency before Phase 5.
- **P0.3** Add `tools/` to the solution as the home for authoring-time utilities;
  confirm tools are excluded from app publish output.

### Phase 1 — Paper design system v2

**Status (2026-08-30):** P1.1–P1.5 are implemented in the current uncommitted working
tree. The sandbox uses generated alpha-PNG cutouts and stepped puppet choreography rather
than SVG/XAML vector scene art. Release build, full tests, formatter, light/dark captures,
an actual reduced-motion launch, and two consecutive clean independent visual-QA passes
are complete. Real keyboard and VoiceOver interaction in this run remains named
unverified because macOS Accessibility control permission is unavailable; Windows native
interaction is also unavailable.

- **P1.1** Materials: grain brush, paper card, cutout frame, tinted shadows as shared
  styles/controls in `App.axaml` + a small `Controls/` folder; both themes; extend
  `DesignSystemTests` contrast checks to the new pairs.
- **P1.2** Tape, stamp, and torn-edge controls with their settle/press choreographies
  and reduced-motion instant states.
- **P1.3** Motion primitives: `SteppedEasing` (frames parameter), a tiny choreography
  helper for sequencing keyframe animations with skip support, and unit tests proving
  step counts and that skip jumps to final values.
- **P1.4** `PaperStage` layered scene container with the fixed nine-layer order,
  anchor-line layout for puppets (head/shoulder/waist/foot), and per-layer transform
  slots. Include a developer-mode stage sandbox page for visual QA.
- **P1.5** Apply materials to the shell: sidebar, Today, Progress, Settings surfaces
  restyled with paper materials (no layout rewrites yet); verify keyboard, VoiceOver
  labels, and reduced motion on the real app.

Gate: all existing tests pass; new contrast and motion tests pass; the app visibly
carries the paper identity with reduced-motion equivalence verified by interaction.

### Phase 2 — Template engine core

- **P2.1** Core contracts in `Linguistics.Core`: `TemplateId`, `LessonTemplateSchema`
  (typed parameter definitions: text, text-map by language, concept ref, example ref,
  asset ref, task ref, option lists), `TemplateInstance`, and validation errors that
  name the offending pack, lesson, and parameter.
- **P2.2** Pack schema extension (`CONTENT_PACK_SPEC.md` owns the durable truth; update
  it): a `lessons/` section binding lesson IDs from the course plan to ordered template
  instances. Schema version bump with decode tests and one failing fixture per new
  validation category.
- **P2.3** Validator extension: unknown template IDs, parameter type mismatches,
  missing required parameters, dangling concept/example/asset/task references, and
  instruction-language coverage all fail with attributable errors.
- **P2.4** App-side `TemplateRegistry` mapping template IDs to renderer factories; a
  renderer receives resolved parameters, the learner's instruction language, motion
  preference, and an outcome callback; it never touches persistence or mastery
  (architecture test enforces this).
- **P2.5** `CourseCatalogBuilder` integration: lessons with authored instances render
  them in order; lessons without fall back to the current generated slides. Determinism
  test: identical packs ⇒ identical lesson/slide/template IDs.
- **P2.6** Template gallery in developer mode (`LINGUISTICS_DEVELOPER_MODE=1`): every
  registered template rendered with synthetic fixture data, cycling outcome states, for
  visual QA without learner data.
- **P2.7** Three proving templates end to end (object-spotlight, picture-match,
  word-order-train — one per family style: scene, recognition, construction) wired into
  a real lesson in the de pack as machine-validated preview content.

Gate: engine round-trips pack → validation → catalog → rendered lesson; the three
templates play with choreography, reduced motion, keyboard, and text-only paths; all
gates green.

**Status (2026-08-30): complete in the current uncommitted working tree.** Schema 2,
the typed contracts and validator, deterministic authored/fallback catalog projection,
the renderer registry, developer gallery, and all three proving templates are wired.
The German café-items lesson contains three ordered machine-validated preview
instances and deliberately omits pack asset references until Phase 3. Real macOS
interaction covered mouse and keyboard paths, outcome cycling, replay, skip, complete
text-only fallbacks, reduced-motion final states, both themes, and the authored lesson;
finishing the preview created no mastery/progress file. Release build, 231 tests, and
formatting are clean. Two consecutive post-fix Codex plus Antigravity Gemini visual-QA
passes reported no material issue. Direct VoiceOver remains named unverified because
macOS Accessibility trust is false, and Windows native interaction remains unavailable.

### Phase 3 — Asset pipeline

- **P3.1** `tools/AssetPipeline` console app (BCL + HttpClient only): search Wikimedia
  Commons and fetch candidates by keyword; filter to public domain, CC0, CC-BY, and
  CC-BY-SA; emit per-asset attribution records (title, author, license, source URL,
  retrieval date, file hash). Authoring-time only; the shipped app never fetches.
- **P3.2** Processing stage: downscale to template budgets, convert to PNG/JPEG, record
  derivative status (crops and background removals of CC-BY-SA sources keep share-alike
  obligations; the record carries the original and the transformation). Background
  removal for cutouts is an authoring step with manual QA; imperfect edges are part of
  the paper style but subjects must stay legible.
- **P3.3** Generated-image lane: locally generated images enter the same manifest with
  provenance `generated`, the generator name, and prompt summary, and are never labeled
  as photographs of real subjects. Follow the existing model-content rule: generated
  assets are drafts until reviewed.
- **P3.4** Pack integration: `assets.json` manifest schema, validator rules (complete
  license record, referenced-only, size caps, hash match), and loader in the app with
  decoded-image caching keyed by pack version.
- **P3.5** Attribution surfaces: a complete image-credits list in Settings and per-scene
  access to the current image's credit; update `docs/content-license.md` and
  third-party notices generation to include asset licenses.
- **P3.6** Seed batch: source and process the assets needed by Phase 2's three
  templates plus the Unit 1–2 vocabulary domains (greetings props, classroom objects,
  numbers, café items), with attribution records reviewed for completeness.

Gate: validator rejects every crafted bad-asset fixture; the app renders seeded assets
offline from the validated pack; credits are visible and complete.

Implementation status, 2026-08-31: P3.1–P3.6 are implemented in the current uncommitted
working tree. The validated German pack contains seven Wikimedia Commons photographs and
five generated paper-stage images (12 unique assets, 1,619,917 bytes); all 248 tests,
locked restore, formatter, Release build, audit, publish inspection, and two consecutive
final budget-corrected Gemini visual-QA passes are clean on macOS. The assets remain Preview material
pending competent content, license, modification, and redistribution review. Direct
VoiceOver and Windows native interaction remain named evidence gaps.

### Phase 4 — Template catalog (58 templates)

Every template item (`P4.T<number>`) has the same definition of done:

1. Schema registered with typed parameters and validator fixtures (one valid, one
   failing per parameter category).
2. Avalonia renderer using Phase 1 materials and `PaperStage` where scenic.
3. Choreography with skip, sub-4-second staging, and a reduced-motion instant state.
4. Full interaction parity: keyboard operable, VoiceOver/Narrator names and live
   regions, text-only equivalent when the template is image- or audio-led, and a
   microphone-free path when speech-led.
5. Outcome reporting to the deterministic evaluator only through the callback contract;
   activity templates never self-score beyond deterministic checks defined in core.
6. Gallery entry with synthetic fixtures for all outcome states.
7. Unit tests for parameter validation and deterministic outcome mapping.

Build in eight waves; each wave is an independent agentic package ending with the full
gate suite and gallery screenshots plus real interaction on at least two templates per
wave.

**Wave A — Scene and story (presentation)**
- **P4.T1** scene-establish — backdrop plate, paper wash, taped location label, cast
  entrance; the opening beat of scenic lessons.
- **P4.T2** object-spotlight — one cutout centered on stage; name, article, and meaning
  reveal in sequence (the core "describe objects" presenter).
- **P4.T3** object-anatomy — one large cutout, taped part-labels land one by one
  (body, clothing, vehicle, room vocabulary).
- **P4.T4** paper-dialogue — two puppets exchange speech bubbles with stepped bobs;
  optional TTS playback per bubble.
- **P4.T5** street-walk — puppet walks a short sidewalk route past labeled storefronts;
  limited stepped walk cycle, torn foreground silhouettes.
- **P4.T6** postcard-story — postcard flips front/back for a short narrative or
  cultural note.
- **P4.T7** photo-album — album spread turns pages of captioned photos (sets of related
  vocabulary in context).
- **P4.T8** culture-plate — museum-catalogue composition: artifact cutout, caption
  card, source credit (uses real Commons artifacts).
- **P4.T9** weather-window — window scene with ambient paper particles (rain strips,
  snow dots, sun glints) for weather and seasons.
- **P4.T10** clock-theatre — paper clock with animating hands for time expressions.

**Wave A status (2026-09-01): complete with named unverified evidence in the current
Phase 4 checkpoint.** P4.T1–P4.T10 have typed schemas, strict per-kind validator
fixtures, registered Avalonia renderers, all-outcome gallery fixtures, bounded replay and
skip choreography, reduced-motion instant states, named controls and live regions,
complete authored text-only routes, callback-only acknowledgement outcomes, and unit
coverage. P4.T2 now reveals word, article, and meaning in sequence. P4.T4 uses the
existing optional installed-system-voice provider for each complete caption, remains
fully usable without it, and never requests a microphone. Locked restore, formatter,
zero-warning Release build, publish inspection, and all 267 tests pass. Fresh macOS
interaction covered Scene Establish and Postcard Story by mouse and keyboard, replay,
skip, outcome reporting, all four gallery outcome states, image-free text-only routes,
reduced-motion final states, and light/dark themes. Local German caption playback also
completed through the installed system voice. Final gallery captures for every Wave A
template are under `artifacts/phase4-wave-a/screenshots/`.

Named follow-ups and evidence gaps: no suitable validated Commons cultural artifact is
bundled, so P4.T8 intentionally renders its authored text-only plate until that exact
asset is sourced and validated; direct VoiceOver and Windows native interaction remain
unverified; competent linguistic, cultural, content, license, modification, and
redistribution review remains pending. No approval, distribution, or release claim is
made.

**Wave B — Vocabulary recognition**
- **P4.T11** picture-match — hear/read a word, pick the matching cutout from 3–4.
- **P4.T12** word-match — see a cutout, pick the word.
- **P4.T13** pair-cards — flip-to-match word↔image pairs, paper card flips.
- **P4.T14** odd-one-out — four cutouts, one doesn't belong.
- **P4.T15** sort-into-baskets — drag (or keyboard-assign) cutouts into labeled paper
  baskets: categories, genders, semantic fields.
- **P4.T16** article-stamp — stamp der/die/das onto a noun cutout; wrong stamp lifts
  off with a wobble.
- **P4.T17** plural-fold — card unfolds from singular to plural form.
- **P4.T18** color-swatch — paint-chip cards for color terms applied to objects.
- **P4.T19** number-tiles — quantity scenes and digit tiles for numbers.
- **P4.T20** label-the-scene — busy backdrop with tappable/tabbable hotspots to label.

**Wave B status (2026-09-01): complete with named unverified evidence in the current
Phase 4 checkpoint.** P4.T11–P4.T20 have typed schemas, strict per-kind validator
fixtures, registered Avalonia renderers, all-outcome gallery fixtures, bounded replay and
skip choreography, reduced-motion instant states, keyboard names and live regions,
complete authored text-only routes, callback-only outcomes, and deterministic core
mapping tests. The existing P4.T11 proving renderer now keeps the written target visible
and offers optional installed-system-voice playback without a microphone. P4.T15 supports
pointer selection, keyboard assignment, and native Avalonia drag/drop while reporting
only the core evaluator result. Wave B needed no new assets and uses only the 12 validated
German Preview images already bundled by Phase 3.

Locked restore, formatter, zero-warning Release build, publish inspection, and all 279
tests pass. The published app contains all 12 validated images and no AssetPipeline
binary. Fresh macOS interaction covered Picture Match and Sort Into Baskets by mouse and
keyboard, deterministic failure/uncertain/success callbacks, replay, skip, local German
playback, all four gallery outcome states, complete image-free text-only routes,
reduced-motion final states, and light/dark themes. Final captures for every Wave B
template plus the interaction variants are under
`artifacts/phase4-wave-b/screenshots/`.

Named follow-ups and evidence gaps: the automation bridge could not synthesize a native
drag gesture, so drag-specific macOS interaction remains unverified even though the
pointer and keyboard assignment paths were exercised; direct VoiceOver and Windows
native interaction remain unverified; competent linguistic, cultural, content, license,
modification, and redistribution review remains pending. No approval, distribution, or
release claim is made.

**Wave C — Sentence and grammar construction**
- **P4.T21** word-order-train — arrange word cutouts on paper train cars; verb-second
  and bracket positions get reserved cars (the German word-order workhorse).
- **P4.T22** gap-card — cloze sentence with draggable word/letter tiles.
- **P4.T23** sentence-fold — accordion strip unfolds to grow a sentence piece by piece.
- **P4.T24** conjugation-wheel — rotating paper wheel aligns person with verb form.
- **P4.T25** case-switchboard — swap the sentence role of a noun; its article card
  flips to the case form.
- **P4.T26** separable-verb-split — the prefix tears off the verb and flies to the
  clause end.
- **P4.T27** question-flip — statement card flips into its question form.
- **P4.T28** negation-strike — place nicht/kein; misplacement wobbles back.
- **P4.T29** preposition-stage — move an object cutout around the scene (auf, unter,
  neben...) and read/say the resulting phrase.
- **P4.T30** sentence-expand — start with subject-verb, add cutout complements to
  build longer sentences.

**Wave C status (2026-09-01): complete with named unverified evidence in the current
Phase 4 checkpoint.** P4.T21–P4.T30 have typed schemas, strict per-kind validator
fixtures, registered Avalonia renderers, all-outcome gallery fixtures, bounded replay and
skip choreography, reduced-motion instant states, keyboard names and live regions,
complete authored text-only routes, callback-only outcomes, and deterministic core
mapping tests. P4.T21 now exposes fixed START, VERB 2, MIDDLE, and RIGHT BRACKET cars.
P4.T22 and P4.T29 share the same callback path for pointer, keyboard, and native
drag/drop inputs. Person/form, role/article, negator/slot, and complement-order checks
submit stable authored IDs to the pure core evaluator. Presentation-only fold, split,
and flip templates report acknowledgement only after their authored interaction is
complete. Wave C needed no new assets and uses only the 12 validated German Preview
images already bundled by Phase 3.

Locked restore, formatter, zero-warning Release build, publish inspection, and all 289
tests pass. The published app contains all 12 validated images and no AssetPipeline
binary. Fresh macOS interaction covered Gap Card and Preposition Stage by mouse and
keyboard, deterministic failure and success callbacks, replay, skip, all four gallery
outcome states, complete image-free text-only routes, reduced-motion final states, and
light/dark themes. Final captures for every Wave C template plus the interaction
variants are under `artifacts/phase4-wave-c/screenshots/`.

Named follow-ups and evidence gaps: the automation bridge moved the pointer but did not
complete an Avalonia native drag/drop gesture, so drag-specific macOS interaction
remains unverified even though pointer and keyboard paths were exercised; direct
VoiceOver and Windows native interaction remain unverified; competent linguistic,
cultural, content, license, modification, and redistribution review remains pending. No
approval, distribution, or release claim is made.

**Wave D — Listening**
- **P4.T31** listen-pick-image — play TTS utterance, pick the matching cutout.
- **P4.T32** listen-order — sequence event cards in the order heard.
- **P4.T33** listen-type — dictation onto a paper typewriter card with deterministic
  tolerance rules from core.
- **P4.T34** minimal-pair-doors — two doors labeled with a minimal pair; walk the
  puppet through the door for the sound heard (ich/ach, ü/u, long/short vowels).
- **P4.T35** listen-route — follow short spoken directions across a paper map.
- **P4.T36** listen-price-tag — hear a price or time; set the tag or clock.
- **P4.T37** dialogue-eavesdrop — watch a puppet dialogue, then answer one
  comprehension check.

**Wave D status (2026-09-01): complete with named unverified evidence in the current
Phase 4 checkpoint.** P4.T31–P4.T37 have typed schemas, strict per-kind validator
fixtures, registered Avalonia renderers, all-outcome gallery fixtures, bounded replay and
skip choreography, reduced-motion instant states, keyboard names and live regions,
complete authored transcripts and text-only routes, callback-only outcomes, and
deterministic core mapping tests. The listening controls optionally play complete
authored text through the existing installed-system-voice provider, remain fully usable
without it, and never request a microphone. Image, event-order, dictation, minimal-pair,
route, price/time, and dialogue choices submit only stable authored values to the core
evaluator. Wave D needed no new assets and uses only the 12 validated German Preview
images already bundled by Phase 3.

Locked restore, formatter, zero-warning Release build, publish inspection, and all 299
tests pass. The published app contains all 12 validated images and no AssetPipeline
binary. Fresh macOS inspection covered all seven Wave D templates. Real interaction on
Listen Type and Listen Route covered mouse and keyboard success/failure paths, replay,
skip, local German playback, all four gallery outcome states, complete text-only routes,
reduced-motion and motion-enabled final states, and light/dark themes. A first independent
visual review identified selected-state contrast and dialogue-layer alignment issues;
both were corrected. A fresh second review passed the corrected dark, light, scenic,
success, and text-only captures with no material defect. Final captures are under
`artifacts/phase4-wave-d/screenshots/`.

Named evidence gaps: direct VoiceOver and Windows native interaction remain unverified;
competent linguistic, cultural, content, license, modification, and redistribution review
remains pending. No approval, distribution, or release claim is made.

**Wave E — Speaking and pronunciation** (all with text-only and microphone-free paths;
honest assessment rules from `SPEECH.md` apply unchanged)
- **P4.T38** echo-stage — listen, then repeat; expected-versus-recognized comparison on
  a paper strip, replay and slower playback.
- **P4.T39** read-aloud-card — read a card aloud; intelligibility-based feedback only.
- **P4.T40** prompt-respond — puppet asks, learner answers by voice or text through the
  existing deterministic evaluation path.
- **P4.T41** syllable-clap — tap the stress rhythm of a word/phrase; deterministic
  timing windows.
- **P4.T42** long-short-vowel — elastic paper stretch visualizes vowel length; learner
  chooses or produces.

**Wave E status (2026-09-02): complete with named unverified evidence in the current
Phase 4 checkpoint.** P4.T38–P4.T42 have typed schemas, strict per-kind validator
fixtures, registered Avalonia renderers, all-outcome gallery fixtures, bounded replay and
skip choreography, reduced-motion instant states, keyboard names and live regions,
complete written prompts and text-only routes, callback-only outcomes, and deterministic
core mapping tests. Echo Stage, Read Aloud Card, and Prompt and Respond keep microphone
audio transient, require an explicit disclosure before any supported local recognition,
and expose complete typed routes that never claim pronunciation scoring. Syllable Clap
submits only authored beat counts and in-memory interval evidence to the core. Long and
Short Vowel keeps production explicitly unscored and submits only an authored contrast
choice. All optional playback uses the installed system voice and no microphone.

Locked restore, formatter, zero-warning Release build, publish inspection, and all 312
tests pass. The published app contains all 12 validated German Preview images and no
AssetPipeline binary. Fresh macOS inspection covered every Wave E template. Real mouse
and keyboard interaction on Syllable Clap and Long and Short Vowel covered deterministic
success and failure paths, local playback, slower playback, replay, skip, all four gallery
outcomes, text-only routes, reduced-motion and motion-enabled final states, and light and
dark themes. Echo Stage's complete typed route was also exercised successfully while the
unconfigured local speech-model state remained honest and usable. Native QA found three
clipped speech-honesty stamps; their local widths were corrected, and a fresh independent
visual review passed the final all-template, outcome, theme, scenic, and text-only
captures. Final captures are under `artifacts/phase4-wave-e/screenshots/`.

Named evidence gaps: direct VoiceOver, Windows native interaction, real microphone
permission/capture, and configured local speech recognition remain unverified; competent
linguistic, cultural, content, license, modification, and redistribution review remains
pending. No approval, distribution, or release claim is made.

**Wave F — Reading and writing**
- **P4.T43** sign-reading — real photographed sign (Commons) with a comprehension
  check.
- **P4.T44** form-fill — a paper form with labeled fields (name, origin, address).
- **P4.T45** note-write — write a short note on stationery against deterministic
  content checks.
- **P4.T46** menu-read — café/restaurant menu extraction task.
- **P4.T47** schedule-read — timetable/opening-hours extraction.
- **P4.T48** spelling-tiles — spell with letter tiles; alphabet and letter-name
  lessons.

**Wave F status (2026-09-02): complete with named unverified evidence in the current
Phase 4 checkpoint.** P4.T43–P4.T48 have typed schemas, strict per-kind validator
fixtures, registered Avalonia renderers, all-outcome gallery fixtures, bounded replay and
skip choreography, reduced-motion instant states, keyboard names and live regions,
complete text-only presentation, callback-only outcomes, and deterministic core mapping
tests. Sign Reading renders its complete authored sign when no validated photograph is
available. Form Fill keeps synthetic field values inside the renderer and submits only
authored field IDs. Note Write submits only matched authored criterion IDs after bounded
normalization. Menu Read and Schedule Read submit stable answer IDs from explicitly
synthetic source cards. Spelling Tiles exposes written German letter names and submits
only the ordered authored tile IDs.

Locked restore, formatter, zero-warning Release build, publish inspection, and all 325
tests pass. The published app contains all 12 validated German Preview images and no
AssetPipeline binary. Fresh macOS inspection covered every Wave F template. Real mouse
and keyboard interaction on Sign Reading, Form Fill, and Spelling Tiles covered
deterministic success and failure paths, field entry, tile add/reset/order behavior,
replay, skip, all four gallery outcomes, text-only routes, reduced-motion and
motion-enabled final states, and light and dark themes. The fresh diagnostic log contains
only successful app-open and profile-load events. An independent visual review passed all
10 final all-template, interaction, theme, and text-only captures. Final captures are
under `artifacts/phase4-wave-f/screenshots/`.

Named evidence gaps: direct VoiceOver and Windows native interaction remain unverified.
Sign Reading still needs a suitable validated Commons sign photograph and therefore uses
its designed authored text-only equivalent. Competent linguistic, cultural, content,
license, modification, and redistribution review remains pending. No approval,
distribution, or release claim is made.

**Wave G — Transfer and explanation** (all consume the existing `TransferRouter`
output; they render bridges, never choose them)
- **P4.T49** bridge-note — the reusable transfer note as a taped margin note with
  source-language badge, explanation, and dismissal (replaces the plain component
  everywhere).
- **P4.T50** false-friend-alarm — interference warning stamped over the tempting
  form.
- **P4.T51** cognate-thread — a string visibly connects the known-language word to the
  target word.
- **P4.T52** contrast-panes — side-by-side known-versus-target structure comparison.

**Wave G status (2026-09-02): complete with named unverified evidence in the current
Phase 4 checkpoint.** P4.T49–P4.T52 have typed schemas, strict per-kind validator
fixtures, registered Avalonia renderers, all-outcome gallery fixtures, bounded replay and
skip choreography, reduced-motion instant states, keyboard names and live regions,
complete intrinsic text-only presentation, callback-only outcomes, and deterministic
core mapping tests. All four consume already projected transfer data; no renderer calls
`TransferRouter`, chooses a bridge, reads learner state, or writes persistence. Bridge
Note reuses `TransferNoteCardView` with the café surface, keeps source, explanation,
caution, explicit confirmation, and dismissal together, and reports only authored action
IDs. False Friend Alarm uses the existing machine-validated noun-capitalization
interference cue rather than inventing a lexical false friend. Cognate Thread keeps the
authored target-frame boundary visible, while Contrast Panes explicitly separates what
transfers from what changes.

Locked restore, formatter, zero-warning Release build, publish inspection, and all 334
tests pass. The published app contains all 12 validated German Preview images and no
AssetPipeline binary. Fresh macOS inspection covered every Wave G template. Real mouse
interaction on Bridge Note and keyboard-only interaction on Contrast Panes covered
acknowledgement, dismissal, replay, skip, callback outcomes, and live-region updates. The
gallery cycled Ready, Success, Uncertain, and Failure. Text-only mode removed all 70
image-role nodes without removing any Wave G content or controls. Reduced-motion replay
and skip showed the complete state immediately, and both light and dark themes were
captured. A first independent visual review identified an obscuring warning stamp and a
clipped comparison label. Both were corrected, protected by renderer tests, and a fresh
light/dark review passed with no regression. Final captures are under
`artifacts/phase4-wave-g/screenshots/`; the isolated diagnostic log contains only
successful app-open and profile-load events.

Named evidence gaps: direct VoiceOver and Windows native interaction remain unverified.
The café runtime is correctly unavailable while the bundled content and asset licenses
remain Preview, so native interaction with the shared note inside that runtime path was
not claimable; its controller and view composition are covered by tests. Competent
linguistic, cultural, content, license, modification, and redistribution review remains
pending. Wave G required no additional asset. No approval, distribution, or release
claim is made.

**Wave H — Scenario, review, and progress**
- **P4.T53** scenario-theatre — the full communicative task scene: goal card, paper
  set, NPC puppet, conversation, retry; wraps the existing café task engine and any
  future task template.
- **P4.T54** consequence-verdict — the paper-animate consequence beat: outcome
  physically affects the puppet, label clears, taped verdict card lands, detailed
  static report remains.
- **P4.T55** review-flash — spaced-review card with recall grading, feeding the
  existing `review-v1` scheduler.
- **P4.T56** recap-scrapbook — lesson recap assembles the lesson's pieces into a
  scrapbook spread.
- **P4.T57** unit-capstone — mission scene chaining several templates against one goal
  (the "Unit N mission" lessons in the course plan).
- **P4.T58** progress-shelf — capability-first progress scene: situations the learner
  can handle rendered as collected paper objects on a shelf (no XP, no streak
  pressure).

Gate per wave: all definition-of-done points for every template; gallery complete;
determinism and validator suites green; two templates per wave verified by real
interaction on macOS (Windows evidence named if unavailable).

### Phase 5 — Many-to-many instruction language

- **P5.1** Schema: learner-facing explanation strings in packs become per-language
  maps (`{"en": ..., "hi": ...}`); target-language strings stay single-language.
  Validator requires complete coverage for every instruction language a pack declares.
  Update `CONTENT_PACK_SPEC.md`.
- **P5.2** `InstructionLanguageSelector` in core: pure function of explanation consent,
  preferred explanation language, reading comfort, and pack-declared instruction
  languages, with a deterministic fallback chain and a structured explanation for
  developer mode. Unit tests for every branch.
- **P5.3** Template and catalog integration: renderers receive the selected instruction
  language; course catalog output becomes reproducible per (target, instruction
  language) pair; determinism tests updated.
- **P5.4** App chrome localization: extract user-facing shell/feature strings into
  .NET resource files with English and Hindi first; language follows the learner's
  instruction language with a Settings override. Devanagari rendering evidence from
  P0.2 attached.
- **P5.5** Hindi instruction content for the existing German pack (machine-validated
  draft, review-gated like all content), exercising the full path: onboard English +
  Hindi, prefer Hindi explanations, and receive Hindi-taught German lessons with
  Hindi-scripted bridge notes from the existing hi-de transfer pack.
- **P5.6** Onboarding and Languages surfaces: present instruction-language choice and
  fallbacks in plain language; changing preferences re-routes lessons without content
  reload tricks.

Gate: the same lesson plays correctly in English-taught and Hindi-taught modes; routing
is deterministic and explained in developer mode; validator enforces coverage; no
target-language content is duplicated per source language.

### Phase 6 — Learn experience redesign

- **P6.1** Course map redesign: the unit list becomes a paper journey — units as
  scrapbook spreads with a visible path, honest authored/planned distinction kept, next
  action leading. Layout, keyboard order, and resize behavior verified.
- **P6.2** Lesson player redesign: `LearnView` slide host becomes a template player
  (progress strip, skip choreography control, template transitions via shared stage
  rather than plain crossfade where scenic); lesson resume and progress persistence
  behavior preserved with existing tests.
- **P6.3** Move remaining C#-built layout in `LearnView.axaml.cs` into axaml/controls
  where it reduces duplication; views stay logic-free per the architecture ratchet.
- **P6.4** Scenario surface: the café task adopts scenario-theatre + consequence-verdict
  (P4.T53/T54) end to end, replacing the current plain conversation layout; retry,
  fallback, and deterministic evaluation behavior unchanged and re-verified.
- **P6.5** Today and Review adopt paper materials and the review-flash template;
  Progress adopts progress-shelf. Capability-first presentation rules from
  `PRODUCT.md` hold.

Gate: complete learner journey (onboard → course map → scenic lesson → scenario →
review → progress) plays through the new experience with reduced-motion, keyboard, and
screen-reader verification and no regression in the persistence and evaluation suites.

### Phase 7 — Content production at scale

Turn `content/plans/german-course-plan.md` (450 lessons) into authored template
lessons, in review-gated batches. Per batch (one unit, 10 lessons):

- **P7.B<unit>** Author template instances for each lesson using the catalog (typical
  lesson: 1 scene opener, 2–3 presentation templates, 3–4 activity templates, 1 recap);
  source and process required assets through the pipeline; write instruction strings in
  English and Hindi; validate; attach claim-level provenance; mark machine-validated
  and leave the runtime review gate in force.
- Batch order: Units 1–9 (A1) first; hold A2+ until A1 review feedback exists.
- Templates found missing or awkward during authoring feed a change item back into
  Phase 4 rather than being worked around with misused templates.

Gate per batch: validator green, lesson plays end to end in both instruction languages,
asset credits complete, honest Preview labeling intact.

### Phase 8 — Production hardening

- **P8.1** Performance: cold start, lesson open latency, animation frame consistency,
  and memory with full asset caches on a low-resource profile; decoded-image cache
  bounds; no regression to the deterministic engines.
- **P8.2** Accessibility sweep of every template against the `PRODUCT.md` checklist:
  keyboard, focus order, screen reader semantics, scalable text, contrast in both
  themes, captions and replay, text-only and microphone-free modes, reduced motion.
- **P8.3** Determinism and architecture ratchet: extend architecture tests to forbid
  renderers touching persistence/mastery and to require reduced-motion coverage for
  every registered template.
- **P8.4** CI evidence: gallery screenshot capture on macOS and Windows runners as
  build artifacts; visual evidence remains labeled as such, not interaction proof.
- **P8.5** License and notices audit covering all bundled assets (including CC-BY-SA
  derivative obligations) before any distribution decision; `QUALITY_AND_RELEASE.md`
  gates apply; release itself still requires separate explicit authorization.

## Decision log

| Decision | Choice | Why |
| --- | --- | --- |
| Animation runtime | Native Avalonia keyframes/transitions + custom stepped easing | No new dependency; product forbids web wrapper |
| Asset format | Raster PNG cutouts + natively drawn paper decorations | Photographic sources are raster anyway; avoids SVG dependency |
| Template authority | Templates render; deterministic core scores | Existing central boundary, unchanged |
| Instruction strings | Per-language maps in packs, validator-enforced coverage | Scales many-to-many without pack duplication |
| Old slide generator | Kept as fallback for unauthored lessons | Catalog never breaks while content catches up |
| Image sourcing | Wikimedia Commons (PD/CC0/CC-BY/CC-BY-SA) + reviewed generated images | License-traceable, fits provenance model |

## Traceability

| Requirement from the brief | Where it lands |
| --- | --- |
| Paper-animate templates with personality | Phases 1, 4, 6 |
| 50–60 templates | Phase 4: 58 templates, waves A–H |
| Wikimedia/generated images as lesson material | Phase 3, consumed in 4 and 7 |
| Complete redesign where needed | Verdict section; Phases 1, 6 |
| Many-to-many languages, best-suited teaching language | Architecture addition 3, Phase 5 |
| Production quality | Design direction, Phase 8 |
| Long agentic workflow list | Stable work-item IDs, per-item gates throughout |
