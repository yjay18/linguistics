# Paper Theatre Experience Plan

Production redesign of the learner-facing experience: a paper-theatre visual identity, a
deterministic lesson-template engine with 58 templates, an image asset pipeline built on
openly licensed sources, and many-to-many language support so any repertoire (for example
English + Hindi + Hinglish) can learn any packed target language (first German) with
instruction in the language that suits the learner best.

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

**Wave H status (2026-09-02): complete with named unverified evidence in the current
Phase 4 checkpoint.** P4.T53–P4.T58 have typed schemas, strict per-kind validator
fixtures, registered Avalonia renderers, all-outcome gallery fixtures, bounded replay and
skip choreography, reduced-motion instant states, keyboard names and live regions,
complete authored text-only presentation, callback-only outcomes, and deterministic core
mapping tests. Scenario Theatre submits one stable authored response ID and keeps retry
local. Consequence Verdict projects an authored result and submits only the selected
action. Review Flash reveals first, then submits an enumerated `review-v1` rating without
calling the scheduler. Recap Scrapbook reports only an authored advisory action. Unit
Capstone validates the authored template chain and ordered prefix without selecting or
running templates. Progress Shelf renders projected evidence groups without reading
profile, mastery, XP, streak, or persistence state.

Locked restore, formatter, zero-warning Release build, publish inspection, and all 347
tests pass (126 app and 221 core). The published app contains all 12 validated German
Preview images and no AssetPipeline binary. Fresh macOS inspection covered every Wave H
template. Real mouse interaction on Scenario Theatre covered failure, retry, success,
replay, and skip. Real keyboard interaction on Review Flash covered reveal and `Again`
then `Good` ratings with visible focus and live status. The gallery cycled Ready,
Success, Uncertain, and Failure. Text-only mode removed all 71 image-role nodes while
retaining every Wave H control. Reduced-motion replay and skip reached complete states
immediately, and both light and dark themes were captured. Native inspection found
scenario-label, capstone-goal, and shelf-label readability defects; all were corrected.
A fresh independent visual review passed all six templates and the light, reduced-motion,
mouse-success, and keyboard-success variants. Final captures are under
`artifacts/phase4-wave-h/screenshots/`; the isolated diagnostic log contains only
successful app-open and profile-load events.

Named follow-ups and evidence gaps: no validated café-worker puppet or café-interior
backdrop is bundled, so Scenario Theatre and Consequence Verdict use their designed
authored paper-text equivalents where that art is absent. Direct VoiceOver and Windows
native interaction remain unverified. Competent linguistic, cultural, content, license,
modification, and redistribution review remains pending. All learner-visible content is
still machine-validated Preview material. No approval, distribution, or release claim is
made. With those named evidence gaps, the 58-template Phase 4 catalog is complete.

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

**Status (2026-09-02): complete with named unverified evidence.** P5.1–P5.6 are
implemented. Content schema 3 separates instruction-language maps from single-copy
target-language text and validates complete declared-language coverage. The pure
`InstructionLanguageSelector` records each candidate and rejection, uses stable
language-code ordering, and covers preferred, eligible-known, target, and unavailable
branches. Catalog, template, scenario, review, Today, and Progress paths receive the
selected instruction language explicitly; identical inputs retain identical lesson,
slide, template, and target-content IDs.

English and Hindi .NET resources now cover learner-facing shell, startup, recovery,
course, lesson, Today, Progress, Review, Settings, privacy, café, pronunciation,
Languages, onboarding, the three pack-authored templates, and shared image-credit
chrome. A persisted Settings override can separate app chrome from lesson instruction.
Languages changes update the saved preference and reroute the already-loaded validated
catalog. Onboarding explains automatic ordering, preferred-language behavior, and the
unavailable state before saving.

The German target pack declares English and Hindi instruction. Every learner-facing map
has both entries while German examples, option labels, speech text, and IDs remain
single-copy. The existing Hindi-to-German transfer pack provides Hindi-scripted learner
explanations, example notes, and negative-transfer warnings. These remain
machine-validated Preview drafts pending competent review; no content or asset is
described as approved.

Locked restore, zero-warning Release build, all 371 tests (133 app and 238 core),
formatter verification, publish, JSON duplicate-key checks, complete instruction-map
coverage checks, resource parity, em-dash checks, and the 25-word learner-copy audit
pass. Native macOS interaction used an isolated profile and covered mouse and keyboard
onboarding, English plus Hindi selection, a Hindi-taught German proving lesson,
Devanagari labels and automation names, deterministic outcome reporting, replay, skip,
and reduced-motion final state. The same German `Kaffee` target and template remained
visible while instruction and chrome changed between Hindi and English. Relaunch showed
the saved English selection without reloading or rewriting content packs.

Named evidence gaps: native QA found stale XAML labels during an in-place language
switch. The binding mechanism was replaced with version-driven reevaluation and its
automated gate passes, but the Mac locked before the final post-fix native switch could
be repeated. That exact interaction remains unverified on the final build. Direct
VoiceOver, Windows native interaction, and a fresh Phase 5 light-theme pass are also
unverified. Developer-only gallery and diagnostics fixture copy remains English and is
not learner-facing. Competent Hindi, German, bilingual transfer, pedagogical, cultural,
content, license, modification, and redistribution review remains pending. With these
named evidence gaps, Phase 5 is complete but not fully verified.

**Hinglish extension status (2026-09-03): complete with named unverified evidence.**
The Phase 5 path now also treats `hi-latn` as an explicit known and instruction
language. Onboarding, Languages, and Settings expose Hinglish as Hindi in Latin script,
persist its reading, listening, explanation, proficiency, and preferred-language
choices, and reroute the already-loaded catalog without changing German target IDs or
ordering. This is static authored copy, not runtime transliteration. Because app chrome
remains localized in English and Devanagari Hindi, a Hinglish-taught lesson uses English
controls while its course, explanation, feedback, and transfer copy use `hi-latn`.

The German target pack and Hindi-to-German transfer pack declare English, Hindi, and
Hinglish with complete learner-facing maps. Hindi transfer mappings remain source
language `hi`; `TransferRouter` deterministically accepts a known `hi-latn` variant from
the same primary language family and prefers the learner's exact script choice before
stable fallback. It still selects only supplied mappings and never authors a bridge.

Locked Release build, all 380 tests (140 app and 240 core), formatter verification,
publish, JSON parsing, complete map coverage, 25-word Hinglish copy checks, no-Devanagari
Hinglish checks, and dash checks pass. Fresh native macOS interaction used an isolated
schema 7 profile. Mouse and keyboard onboarding selected Hinglish, exercised preferred
routing, microphone Never, reduced motion, summary, save, course projection, an
ineligible Languages state, restore, save, persistence, and Settings. Light and dark
relaunches retained static Hinglish lesson titles and English chrome.

Direct VoiceOver and Windows native interaction remain unverified. Competent Hinglish,
Hindi, German, bilingual-transfer, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. All new learner-visible copy
is machine-validated Preview material and is not described as approved.

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

**Status (2026-09-03): complete with named unverified evidence.** P6.1–P6.5 are
implemented. Learn now presents the 13 available Preview lessons as a paper journey with
a deterministic next marker, scrapbook unit spreads, a visible authored-template label,
and an honest 437-lesson planned path. The focused lesson player has one progress strip,
centralized replay and skip controls, shared-stage scenic transitions, and AXAML-backed
guided cards. Preview visits still write no mastery, and existing resume and persistence
boundaries are unchanged.

The production café view composes Scenario Theatre for the live authored exchange and
Consequence Verdict for its deterministic result. Missing café-worker and interior art
uses the complete authored text-only paper set and character card. Retry, fallback,
speech, transfer, evaluation, and atomic-save authority remain in the existing controller
and core. Review composes Review Flash, maps only its stable rating IDs to the existing
`ReviewRating` contract, and leaves scheduling to `ReviewController`. Progress projects
the existing `LearningProgressOverview` into Progress Shelf status groups; the renderer
does not read or infer mastery. Today keeps capability-first paper materials and wraps its
evidence row at narrower widths.

Locked restore, a zero-warning Release build, all 378 tests (140 app and 238 core),
formatter verification, publish, and publish inspection pass. The publish contains the
12 validated local Preview images and no AssetPipeline binary. Fresh native macOS
interaction used an isolated profile and covered mouse and keyboard onboarding, a
microphone setting of Never, reduced motion, the course map at two window sizes, guided
and authored lesson cards, centralized replay and keyboard skip, deterministic failure
and success outcomes, the Today route, and Progress Shelf selection. Developer gallery
interaction covered Scenario Theatre failure, retry, and success plus Review Flash
replay, keyboard skip, reveal, and Good rating. Learn and Progress were inspected in both
light and dark themes. The macOS accessibility tree exposed named navigation, lesson
progress, goals, controls, text alternatives, live outcome text, review choices, and
capability status.

Named evidence gaps: the installed German content and assets remain machine-validated
Preview material, so the production Scenario and Review routes correctly fail closed.
The complete production onboarding-to-scenario-to-review journey cannot be exercised
until competent linguistic and license approval exists; the developer fixtures verify
the composed renderer interactions without claiming persistence. Direct VoiceOver is
unverified. Windows native work is intentionally deferred under the current macOS-only
scope. A validated café-worker puppet and café-interior backdrop remain named asset
follow-ups. Competent German, Hindi, bilingual-transfer, pedagogical, cultural, content,
license, modification, and redistribution review remains pending. With these named
evidence gaps, Phase 6 is complete but not fully verified.

### Phase 7 — Content production at scale

Turn `content/plans/german-course-plan.md` (450 lessons) into authored template
lessons, in review-gated batches. Per batch (one unit, 10 lessons):

- **P7.B<unit>** Author template instances for each lesson using the catalog (typical
  lesson: 1 scene opener, 2–3 presentation templates, 3–4 activity templates, 1 recap);
  source and process required assets through the pipeline; write instruction strings in
  English, Hindi, and Hinglish; validate; attach claim-level provenance; mark
  machine-validated and leave the runtime review gate in force.
- Batch order: Units 1–9 (A1) first; hold A2+ until A1 review feedback exists.
- Templates found missing or awkward during authoring feed a change item back into
  Phase 4 rather than being worked around with misused templates.

Gate per batch: validator green, lesson plays end to end in every declared instruction
language, asset credits complete, honest Preview labeling intact.

**Unit 1 batch status (2026-09-03): complete with named unverified evidence.**
P7.B1 is authored directly without local-model lesson generation. The batch contains
10 ordered A1 lessons and 79 template instances, with 10 concepts, 26 lexicon entries,
one deterministic first-meeting task, 10 error rules, 10 feedback templates, one
rubric, and 10 pronunciation text scripts. Every lesson opens with Scene Establish,
ends with Recap Scrapbook, and stays machine-validated Preview content. English,
Hindi, and authored Latin-script Hinglish maps cover learner-facing instructions while
German examples and deterministic answer IDs remain single-copy.

The batch uses the existing catalog and validated local asset policy. No new media was
needed: image- and audio-led surfaces expose their complete authored text equivalent,
and speech-led practice retains a typed path with no microphone or pronunciation score
required. Claim-level source records cite the consulted CEFR, Goethe-Institut, and
COERLL materials. The deterministic core still owns outcomes, task checks, IDs, order,
and progression; template renderers only report callback outcomes. Native QA exposed a
café-specific label in the reusable Scenario Theatre fallback, which is now the neutral
`SCENARIO SET` with the automation name `Paper scenario set` and a regression test.

Formatter verification, a zero-warning Release build, all 384 tests (140 app and 244
core), publish, JSON parsing, domain-ID uniqueness, stable 1-to-10 lesson ordering,
machine-validation status, no-Devanagari Hinglish, dash, and whitespace checks pass.
Fresh native macOS interaction used an isolated schema 7 profile with English, Hindi,
and Hinglish enabled, microphone Never, and reduced motion. English lessons 1, 6, and
10 played end to end. The checks covered mouse and keyboard navigation, replay, skip,
failure and success outcomes, retry, listening transcripts, typed speech practice,
the four-step capstone, and lesson completion. Hindi lesson 6 and Hinglish lesson 6
rendered their authored instruction maps; Hinglish lesson 10 advanced through the
corrected Scenario Theatre, microphone-free response, and completed capstone. The
course and mission were inspected in light and dark themes, and the macOS accessibility
tree exposed named controls, text equivalents, statuses, and live outcome regions.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end native
playthrough of all 10 lessons in each of Hindi and Hinglish remains unverified; current
evidence combines complete English playthroughs, focused Hindi and Hinglish rendering,
and validator coverage. Competent German, Hindi, Hinglish, pedagogical, cultural,
content, license, modification, and redistribution review remains pending. No bundled
lesson is described as approved. The exact next Phase 7 step is P7.B2, Unit 2's 10
review-gated lessons, after preserving this Unit 1 checkpoint.

**Unit 2 batch status (2026-09-04): complete with named unverified evidence.**
P7.B2 is authored directly without local-model lesson generation. The batch contains
10 ordered A1 lessons and 80 template instances across 28 catalog kinds, with 10
concepts, 26 lexicon entries, one deterministic classroom-help task, 10 error rules,
10 feedback templates, one rubric, and 10 pronunciation text scripts. Every lesson
opens with Scene Establish, ends with Recap Scrapbook, and remains machine-validated
Preview content. English, Hindi, and authored Latin-script Hinglish maps cover all
learner-facing instructions while German examples, answer IDs, ordering, and outcomes
remain deterministic single-copy data.

The batch uses only the existing catalog and validated local asset policy. No new media
was needed: image- and audio-led surfaces show complete authored text equivalents, and
speech-led practice keeps a typed route with no microphone or pronunciation score
required. Four claim-level source records cite the consulted CEFR, Goethe-Institut,
and COERLL materials. Native QA found that the course journey replaced authored unit
titles and descriptions with a dominant-concept category label. The journey now
preserves the validated pack title and description, including its selected instruction
language, with a regression test for the projected title, description, and automation
name.

Formatter verification, a zero-warning Release build, all 385 tests (140 app and 245
core), publish, JSON parsing, stable 11-to-20 lesson ordering, 80-instance counts,
machine-validation status, instruction-map coverage, no-Devanagari Hinglish, dash,
whitespace, and 25-word copy checks pass. Fresh native macOS interaction used an
isolated schema 7 profile with English, Hindi, and Hinglish enabled, microphone Never,
and reduced motion. English lessons 11 and 15 and Hinglish lesson 20 played end to end.
The checks covered mouse and keyboard navigation, replay, skip, deterministic failure
and success outcomes, scenario retry, local listening transcripts, typed speech paths,
the ordered four-step capstone, and completion. Hindi rendered the complete course map
and lesson 18 through its text-only sign activity. Light and dark interaction covered
the course, Unit 2 scenic openers, replay, skip, and reduced-motion final states. The
macOS accessibility tree exposed authored unit names, named controls, text equivalents,
statuses, and live outcome regions.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end native
playthrough of all 10 lessons in each declared instruction language remains unverified;
current evidence combines complete representative English and Hinglish playthroughs,
focused Hindi interaction, deterministic unit tests, and complete validator coverage.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No bundled lesson is described
as approved. The exact next Phase 7 step is P7.B3, Unit 3's 10 review-gated lessons.

**Unit 3 batch status (2026-09-04): complete with named unverified evidence.**
P7.B3 implementation is complete and directly authored without local-model lesson
generation. The pack contains 10 ordered A1 lessons and 80 template instances across
29 catalog kinds, with 10 concepts, 26 lexicon entries, one deterministic scheduling
task, 10 error rules, 10 feedback templates, one rubric, and 10 pronunciation text
scripts. Every lesson opens with Scene Establish, ends with Recap Scrapbook, and stays
machine-validated Preview content. English, Hindi, and authored Latin-script Hinglish
maps cover learner-facing instructions while German examples, answer IDs, ordering,
and outcomes remain deterministic single-copy data.

The batch uses only the existing catalog. No new media was needed: image- and
audio-led surfaces expose complete authored text equivalents, and speech-led practice
retains a typed route without microphone or pronunciation scoring. Five claim-level
source records cite the consulted CEFR, Goethe-Institut, and COERLL materials. JSON
parsing, stable 21-to-30 lesson ordering, 80-instance counts, first and last template
roles, machine-validation status, instruction-map coverage, no-Devanagari Hinglish,
dash, whitespace, 25-word copy, deterministic regeneration, and published-content
inspection pass. Formatter verification, a zero-warning Release build, publish, all
386 tests (140 app and 246 core), and focused deterministic outcome and three-language
projection tests pass.

Fresh native macOS interaction used an isolated schema 7 profile with English, Hindi,
and Hinglish enabled, microphone Never, and reduced motion. English lesson 21 and
Hinglish lesson 30 played end to end. The checks covered mouse and keyboard navigation,
replay, skip, deterministic failure and success outcomes, retry, album navigation,
complete listening transcripts, typed microphone-free speech practice, an accepted
Scenario Theatre response, the ordered four-step capstone, recap, and completion. Hindi
rendered the complete course map and lesson 28 through its authored text-only opening-
hours sign and retry. Light and dark interaction covered the course, Unit 3 scenic
openers, replay, skip, and reduced-motion final states. The macOS accessibility tree
exposed authored unit names, named controls, complete text equivalents, statuses, and
live outcome regions.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end native
playthrough of all 10 lessons in each declared instruction language remains unverified;
current evidence combines complete representative English and Hinglish playthroughs,
focused Hindi interaction, deterministic unit tests, and complete validator coverage.
Catalog-owned fixed labels and status copy outside the Phase 5 proving templates remain
English in some Hindi-taught renderers; authored instructions and live outcomes switch
correctly, and full renderer-chrome localization is a named P5.4 follow-up before
content approval. Competent German, Hindi, Hinglish, pedagogical, cultural, content,
license, modification, and redistribution review remains pending. No bundled lesson is
described as approved. The exact next Phase 7 step is P7.B4, Unit 4's 10 review-gated
lessons, after preserving this Unit 3 checkpoint.

**Unit 4 batch status (2026-09-04): complete with named unverified evidence.**
P7.B4 is authored directly without local-model lesson generation. The batch contains
10 ordered A1 lessons and 80 template instances across 25 catalog kinds, with 10
concepts, 32 lexicon entries, one deterministic relationship-map task, 10 error rules,
10 feedback templates, one rubric, and 10 pronunciation text scripts. Every lesson
opens with Scene Establish, ends with Recap Scrapbook, and remains machine-validated
Preview content. English, Hindi, and authored Latin-script Hinglish maps cover all
learner-facing instructions while German examples, answer IDs, ordering, and outcomes
remain deterministic single-copy data.

The batch uses only the existing catalog and validated local asset policy. No new media
was needed: image- and audio-led surfaces expose complete authored text equivalents,
and speech-led practice retains a typed route without microphone or pronunciation
scoring. Five claim-level source records cite the consulted CEFR, Goethe-Institut, and
COERLL materials for people, family, work, and pronunciation. The deterministic core
still owns task checks, outcomes, IDs, order, and progression; renderers only report
callback outcomes, and Preview visits do not change mastery.

JSON parsing, stable 31-to-40 lesson ordering, 80-instance counts, first and last
template roles, machine-validation status, complete instruction maps, no-Devanagari
Hinglish, dash, whitespace, 25-word copy, deterministic regeneration, and published-
content inspection pass. Formatter verification, a zero-warning Release build,
publish, all 387 tests (140 app and 247 core), and focused deterministic outcome and
three-language projection tests pass.

Fresh native macOS interaction used an isolated schema 7 profile with English, Hindi,
and Hinglish enabled, microphone Never, and reduced motion. English lessons 31 and 40
were exercised through their recaps, with lesson 40 returning to the course map. The
checks covered mouse and keyboard navigation, replay, skip, deterministic failure and
success outcomes, retry, album navigation, form entry, complete listening captions,
typed microphone-free speech practice, Scenario Theatre correction, the ordered four-
step capstone, and the relationship-map mission. Hindi rendered the complete Unit 4
course map and lesson 38 through its text-only profile card and deterministic success
state. Hinglish rendered the complete Latin-script course map and lesson 39 through a
scenic opener, captioned dialogue, skip, and deterministic success state. Light and
dark interaction covered the course and Unit 4 scenic openers; the dark relationship
map remained legible, and replay plus skip exposed the reduced-motion final state. The
macOS accessibility tree exposed authored names, controls, text equivalents, statuses,
and live outcome regions.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end native
playthrough of all 10 lessons in each declared instruction language remains unverified;
current evidence combines representative English playthroughs, focused Hindi and
Hinglish interaction, deterministic unit tests, and complete validator coverage.
Catalog-owned fixed labels and status copy remain English in some Hindi- and Hinglish-
taught renderers; authored instructions and live outcomes switch correctly, and full
renderer-chrome localization remains the named P5.4 follow-up before content approval.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No bundled lesson is
described as approved. The exact next Phase 7 step is P7.B5, Unit 5's 10 review-gated
lessons, after preserving this Unit 4 checkpoint.

**Unit 5 batch status (2026-09-04): complete with named unverified evidence.**
P7.B5 is authored directly without local-model lesson generation. The batch contains
10 ordered A1 lessons and 80 template instances across 26 catalog kinds, with 10
concepts, 32 lexicon entries, one deterministic routine-planning task, 10 error rules,
10 feedback templates, one rubric, and 10 pronunciation text scripts. Every lesson
opens with Scene Establish, ends with Recap Scrapbook, and remains machine-validated
Preview content. English, Hindi, and authored Latin-script Hinglish maps cover all
learner-facing instructions while German examples, answer IDs, ordering, and outcomes
remain deterministic single-copy data.

The batch uses only the existing catalog and validated local asset policy. No new media
was needed: image- and audio-led surfaces expose complete authored text equivalents,
and speech-led practice retains a typed route without microphone or pronunciation
scoring. Five claim-level source records cite the consulted CEFR, Goethe-Institut, and
COERLL materials for daily actions, separable verbs, routine language, and sentence
stress. The deterministic core still owns task checks, outcomes, IDs, order, and
progression; renderers only report callback outcomes, and Preview visits do not change
mastery.

JSON parsing, stable 41-to-50 lesson ordering, 80-instance counts, first and last
template roles, machine-validation status, complete instruction maps, no-Devanagari
Hinglish, dash, placeholder, whitespace, deterministic regeneration, and published-
content inspection pass. Formatter verification, a zero-warning Release build,
publish, all 388 tests (140 app and 248 core), and focused deterministic outcome and
three-language projection tests pass.

Fresh native macOS interaction used an isolated schema 7 profile with English, Hindi,
and Hinglish enabled, microphone Never, and reduced motion. English lessons 41 and 50
played end to end and returned to the course map. The checks covered mouse and keyboard
navigation, replay, skip, deterministic failure and success outcomes, retry, album
navigation, basket sorting, form entry, complete listening captions, typed microphone-
free speech practice, Scenario Theatre correction, the ordered four-step capstone, and
recap completion. Hindi rendered the complete Unit 5 course map and lesson 48 through
its calendar choice, two-sided text-only postcard, and completed synthetic form.
Hinglish rendered the complete Latin-script course map and lesson 49 through its scenic
opener, two-page text album, basket sorting, and deterministic success state. Light and
dark interaction covered the course and Unit 5 scenic opener; replay plus skip exposed
the reduced-motion final state. The macOS accessibility tree exposed authored names,
controls, text equivalents, statuses, and live outcome regions.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end native
playthrough of all 10 lessons in each declared instruction language remains unverified;
current evidence combines complete representative English playthroughs, focused Hindi
and Hinglish interaction, deterministic unit tests, and complete validator coverage.
Catalog-owned fixed labels and status copy remain English in some Hindi- and Hinglish-
taught renderers; authored instructions and live outcomes switch correctly, and full
renderer-chrome localization remains the named P5.4 follow-up before content approval.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No bundled lesson is
described as approved. The exact next Phase 7 step is P7.B6, Unit 6's 10 review-gated
lessons, after preserving this Unit 5 checkpoint.

**Unit 6 batch status (2026-09-04): complete with named unverified evidence.**
P7.B6 is authored directly without local-model lesson generation. The batch contains
10 ordered A1 lessons and 80 template instances across 31 catalog kinds, with 10
concepts, 32 lexicon entries, one deterministic café-order task, 10 error rules, 10
feedback templates, one rubric, and 10 pronunciation text scripts. Every lesson opens
with Scene Establish, ends with Recap Scrapbook, and remains machine-validated Preview
content. English, Hindi, and authored Latin-script Hinglish maps cover all learner-
facing instructions while German examples, answer IDs, ordering, and outcomes remain
deterministic single-copy data.

The batch uses only existing catalog templates and validated local German-pack assets.
No new media was added: image- and audio-led surfaces expose complete authored text
equivalents, and speech-led practice retains a typed route without microphone or
pronunciation scoring. A suitable reviewed café or menu sign photograph remains a
named asset follow-up; Sign Reading exposes the complete authored sign instead of a
placeholder or network fetch. Six claim-level source records cite the consulted CEFR,
Goethe-Institut, and COERLL materials for food, café ordering, case, and pronunciation.
The deterministic core still owns task checks, outcomes, IDs, order, and progression;
renderers only report callback outcomes, and Preview visits do not change mastery.

JSON parsing, stable 51-to-60 lesson ordering, 80-instance counts, first and last
template roles, machine-validation status, complete instruction maps, no-Devanagari
Hinglish, dash, placeholder, whitespace, 25-word copy, byte-stable regeneration, and
published-content inspection pass. Formatter verification, a zero-warning Release
build, publish, all 389 tests (140 app and 249 core), and focused deterministic outcome
and three-language projection tests pass.

Fresh native macOS interaction used isolated schema 7 profiles with English, Hindi,
and Hinglish enabled, microphone Never, and reduced motion. English lessons 51 and 60
played end to end and returned to the course map. The checks covered mouse and keyboard
navigation, replay, skip, deterministic failure and success outcomes, retry, card
matching, menu-price correction, form entry, complete listening text, typed microphone-
free speech practice, the ordered four-step capstone, and recap completion. Hindi
rendered the complete Unit 6 course map and lesson 58 through its explicit unavailable-
asset state, authored sign text, menu correction, basket sorting, written listening
alternative, and completed synthetic form. Hinglish rendered the complete Latin-script
course map and lesson 59 through its album, captioned dialogue, note, completed group-
order form, Scenario Theatre failure and recovery, and microphone-free typed response.
Light and dark interaction covered the course and scenic openers; replay plus skip
exposed the reduced-motion final state. The macOS accessibility tree exposed authored
names, controls, text equivalents, statuses, and live outcome regions.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end native
playthrough of all 10 lessons in each declared instruction language remains unverified;
current evidence combines complete representative English playthroughs, focused Hindi
and Hinglish interaction, deterministic unit tests, and complete validator coverage.
Catalog-owned fixed labels and status copy remain English in some Hindi- and Hinglish-
taught renderers; authored instructions and live outcomes switch correctly, and full
renderer-chrome localization remains the named P5.4 follow-up before content approval.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No bundled lesson is
described as approved. The exact next Phase 7 step is P7.B7, Unit 7's 10 review-gated
lessons, after preserving this Unit 6 checkpoint.

**Unit 7 batch status (2026-09-04): complete with named unverified evidence.**
P7.B7 is authored directly without local-model lesson generation. The batch contains
10 ordered A1 lessons and 80 template instances across 32 catalog kinds, with 10
concepts, 32 lexicon entries, one deterministic home-layout task, 10 error rules, 10
feedback templates, one rubric, and 10 pronunciation text scripts. Every lesson opens
with Scene Establish, ends with Recap Scrapbook, and remains machine-validated Preview
content. English, Hindi, and authored Latin-script Hinglish maps cover all learner-
facing instructions while German examples, answer IDs, ordering, and outcomes remain
deterministic single-copy data.

The batch uses only existing catalog templates and validated local German-pack assets.
No new media was added: image- and audio-led surfaces expose complete authored text
equivalents, and speech-led practice retains a typed route without microphone or
pronunciation scoring. Suitable reviewed home photography and a room backdrop remain
named asset follow-ups; affected templates render their complete authored text instead
of placeholders or network fetches. Seven claim-level source records cite the consulted
CEFR, Goethe-Institut, and COERLL materials for rooms, belongings, location, case, and
pronunciation. The deterministic core still owns task checks, outcomes, IDs, order, and
progression; renderers only report callback outcomes, and Preview visits do not change
mastery.

JSON parsing, stable 61-to-70 lesson ordering, 80-instance counts, first and last
template roles, machine-validation status, complete instruction maps, no-Devanagari
Hinglish, dash, placeholder, whitespace, 25-word copy, byte-stable regeneration, and
published-content inspection pass. Formatter verification, a zero-warning Release
build, publish, all 390 tests (140 app and 250 core), and focused deterministic outcome
and three-language projection tests pass.

Fresh native macOS interaction used isolated schema 7 profiles with English, Hindi,
and Hinglish enabled, microphone Never, and reduced motion. English lessons 61 and 70
played end to end and returned to the course map. The checks covered mouse and keyboard
navigation, replay, skip, deterministic failure and success outcomes, retry, matching,
ordered listening, exact form entry, complete written alternatives, typed microphone-
free speech practice, the ordered four-step capstone, and recap completion. Hindi
rendered the complete Unit 7 course map and lesson 68 through its explicit unavailable-
asset state, authored rental-sign text, price correction, form, written listening
alternative, and German note. Hinglish rendered the complete Latin-script Unit 7 card
set and lesson 69 through its text album, captioned dialogue, preposition stage, note,
form, Scenario Theatre failure and recovery, and recap. Light and dark interaction
covered the course and scenic openers; replay plus skip exposed the reduced-motion final
state. The macOS accessibility tree exposed authored names, controls, text equivalents,
statuses, and live outcome regions.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end native
playthrough of all 10 lessons in each declared instruction language remains unverified;
current evidence combines complete representative English playthroughs, focused Hindi
and Hinglish interaction, deterministic unit tests, and complete validator coverage.
Catalog-owned fixed labels and status copy remain English in some Hindi- and Hinglish-
taught renderers; authored instructions and live outcomes switch correctly, and full
renderer-chrome localization remains the named P5.4 follow-up before content approval.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No bundled lesson is
described as approved. The exact next Phase 7 step is P7.B8, Unit 8's 10 review-gated
lessons, after preserving this Unit 7 checkpoint.

**Unit 8 batch status (2026-09-04): complete with named unverified evidence.**
P7.B8 is authored directly without local-model lesson generation. The batch contains
10 ordered A1 lessons and 80 template instances across 30 catalog kinds, with 10
concepts, 32 lexicon entries, one deterministic town-navigation task, 10 error rules,
10 feedback templates, one rubric, and 10 pronunciation text scripts. Every lesson
opens with Scene Establish, ends with Recap Scrapbook, and remains machine-validated
Preview content. English, Hindi, and authored Latin-script Hinglish maps cover all
learner-facing instructions while German examples, answer IDs, ordering, and outcomes
remain deterministic single-copy data.

The batch uses only existing catalog templates and validated local German-pack assets.
No new media or asset references were added: image- and audio-led surfaces expose
complete authored text equivalents, and speech-led practice retains a typed route
without microphone or pronunciation scoring. A reviewed town map, station display,
and route backdrop remain named asset follow-ups; affected templates render their
complete authored text instead of placeholders or network fetches. Seven claim-level
source records cite the consulted CEFR, Goethe-Institut, Deutsch im Blick, and Grimm
materials for places, transport, directions, dative usage, and pronunciation. The
deterministic core still owns task checks, outcomes, IDs, order, and progression;
renderers only report callback outcomes, and Preview visits do not change mastery.

JSON parsing, stable 71-to-80 lesson ordering, 80-instance counts, first and last
template roles, machine-validation status, complete instruction maps, no-Devanagari
Hinglish, dash, placeholder, whitespace, 25-word copy, byte-stable regeneration, and
published-content inspection pass. Formatter verification, a zero-warning Release
build, publish, all 391 tests (140 app and 251 core), and focused deterministic outcome
and three-language projection tests pass. The generated and published pack both have
SHA-256 `4a9e88356d30c665c855aaeb54c42619e6562b9c61ae7f967603bbed2964f44a`.

Fresh native macOS interaction used isolated schema 7 profiles with English, Hindi,
and Hinglish enabled, microphone Never, and reduced motion. English lessons 71 and 80
played end to end and returned to the course map. The checks covered mouse and keyboard
navigation, replay, skip, deterministic failure and success outcomes, retry, matching,
ordered route listening, exact form entry, complete written alternatives, typed
microphone-free speech practice, the ordered four-step capstone, and recap completion.
Hindi rendered the complete Unit 8 course map and lesson 78 through its unavailable-
photo state, authored route display, timetable and word corrections, written listening
alternative, completed form, and German note. Hinglish rendered the complete Latin-
script Unit 8 card set and lesson 79 through its street walk, captioned dialogue,
ordered written route, note, form, Scenario Theatre failure and recovery, and recap.
Light and dark interaction covered the course and scenic openers; replay plus skip
exposed the reduced-motion final state. The dark app required its documented developer
theme variable at launch and then rendered the dark paper palette correctly. The macOS
accessibility tree exposed authored names, controls, text equivalents, statuses, and
live outcome regions. Isolated diagnostics contained only successful app-open and
profile-load events.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end native
playthrough of all 10 lessons in each declared instruction language remains unverified;
current evidence combines complete representative English playthroughs, focused Hindi
and Hinglish interaction, deterministic unit tests, and complete validator coverage.
Catalog-owned fixed labels and status copy remain English in some Hindi- and Hinglish-
taught renderers; authored instructions and live outcomes switch correctly, and full
renderer-chrome localization remains the named P5.4 follow-up before content approval.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No bundled lesson is
described as approved. The exact next Phase 7 step is P7.B9, Unit 9's 10 review-gated
lessons, after preserving this Unit 8 checkpoint.

**Unit 9 batch status (2026-09-04): complete with named unverified evidence.**
P7.B9 is authored directly without local-model lesson generation. The batch contains
10 ordered A1 lessons and 80 template instances across 30 catalog kinds, with 10
concepts, 32 lexicon entries, one deterministic integrated-day task, 10 error rules,
10 feedback templates, one rubric, and 10 pronunciation text scripts. Every lesson
opens with Scene Establish, ends with Recap Scrapbook, and remains machine-validated
Preview content. English, Hindi, and authored Latin-script Hinglish maps cover all
learner-facing instructions while German examples, answer IDs, ordering, and outcomes
remain deterministic single-copy data.

The batch uses only existing catalog templates and adds no asset references. Image-
and audio-led surfaces expose complete authored text equivalents, and speech-led
practice retains a typed route without microphone or pronunciation scoring. A reviewed
public-message board, integrated day plan, and cafe-day backdrop remain named asset
follow-ups; affected templates render complete authored text instead of placeholders
or network fetches. Seven claim-level source records cite the consulted CEFR,
Goethe-Institut, Deutsch im Blick, and Grimm materials for integrated A1 language,
public messages, conversational repair, and pronunciation. The deterministic core
still owns task checks, outcomes, IDs, order, and progression; renderers only report
callback outcomes, and Preview visits do not change mastery.

JSON parsing, stable 81-to-90 lesson ordering, 80-instance counts, first and last
template roles, machine-validation status, complete instruction maps, no-Devanagari
Hinglish, dash, placeholder, whitespace, 25-word copy, byte-stable regeneration, and
published-content inspection pass. Locked restore, formatter verification, a zero-
warning Release build, publish, all 394 tests (142 app and 252 core), and focused
deterministic outcome, responsive capstone, paper-stage alignment, and three-language
projection tests pass. The generated and published pack are byte-identical with
SHA-256 `dc6eac3eeca7f6003a6ca0c713c7656325a2f5efae1c9a35b879ede500397e39`.

Fresh native macOS interaction used isolated schema 7 profiles with English, Hindi,
and Hinglish enabled, microphone Never, and reduced motion. English lessons 81 and 90
played end to end and returned to the course map. The checks covered mouse and keyboard
navigation, replay, skip, deterministic failure and recovery, matching, ordered
listening, exact form entry, complete written alternatives, typed microphone-free
speech practice, the ordered four-step capstone, and recap completion. Hindi rendered
the complete Unit 9 course map and lesson 85 through its explicit unavailable-asset
state, authored sign, timetable choice, text album, matching, form, note, recap, and
course-map return. Hinglish rendered the complete Latin-script Unit 9 card set and
lesson 89 through its scene, text album, schedule choice, full transcript,
no-microphone state, ordered written route, and form entry. Native QA exposed a crowded
object spotlight, an overlong scenario role, a four-step capstone overlap, and
over-specific Note Write success copy. The layouts and copy were corrected and covered
by regression tests. The macOS accessibility tree exposed authored names, controls,
text equivalents, statuses, and live outcome regions. Isolated light-profile
diagnostics contained only successful app-open and profile-load events, and Preview
visits left every learning-history collection empty.

Named evidence gaps: the Mac locked before lesson 89's final form outcome, Scenario
Theatre correction, recap, and course-map return could be repeated on the final build.
The same lock prevented Unit 9 dark-theme native interaction; the dark build and
isolated profile exist, but no dark interaction is claimed. Direct VoiceOver remains
unverified. Windows native interaction is intentionally deferred under the current
macOS-only scope. A full end-to-end native playthrough of all 10 lessons in each
declared instruction language remains unverified; current evidence combines complete
representative English and Hindi playthroughs, focused Hinglish interaction,
deterministic unit tests, and complete validator coverage. Catalog-owned fixed labels
and status copy remain English in some Hindi- and Hinglish-taught renderers; authored
instructions and live outcomes switch correctly, and full renderer-chrome localization
remains the named P5.4 follow-up before content approval. Competent German, Hindi,
Hinglish, pedagogical, cultural, content, license, modification, and redistribution
review remains pending. No bundled lesson is described as approved.

Units 1 through 9 now provide the complete 90-lesson A1 authoring checkpoint with 719
deterministic template instances. Phase 7 must hold A2 content until competent review
feedback exists for this A1 corpus. The next Phase 7 action is that review and either
documented approval or authored corrections; it is not further lesson generation.

**Unit 10 batch status (2026-09-05): complete with named unverified evidence.**
The user cleared the A1-to-A2 authoring gate on 2026-09-05 and directed work to begin
on A2. P7.B10 is authored directly without local-model lesson generation. The batch
contains 10 ordered A2 lessons and 80 template instances across 27 catalog kinds,
with 10 concepts, 32 lexicon entries, one deterministic weekend-exchange task, 10
error rules, 10 feedback templates, one rubric, and 10 unscored pronunciation text
scripts. Every lesson opens with Scene Establish, ends with Recap Scrapbook, and
remains machine-validated Preview content. English, Hindi, and authored Latin-script
Hinglish maps cover learner-facing instructions while German examples, answer IDs,
ordering, outcomes, and the dependency chain from Unit 9 remain deterministic
single-copy data.

The batch uses only existing catalog templates and adds no asset references. Image-
and audio-led surfaces expose complete authored text equivalents, and speech-led
practice retains a typed route with the microphone set to Never and no pronunciation
score required. A reviewed weekend timeline, personal-update card, and conversation
backdrop remain named asset follow-ups; the affected templates render authored text
instead of placeholders or network content. Seven claim-level source records cite
the CEFR Companion Volume, Goethe-Institut A2 training and vocabulary materials,
IDS Grammis guidance for participles and auxiliaries, Deutsch im Blick, and
Goethe-Institut final-consonant guidance. Competent review and all license gates
remain in force.

JSON parsing, stable 91-to-100 lesson ordering, 80-instance and 27-template-kind
counts, first and last template roles, complete three-language instruction maps,
machine-validation status, no-Devanagari Hinglish, dash, placeholder, whitespace,
25-word copy, deterministic outcome, and byte-stable regeneration checks pass.
Locked restore, formatter verification, a zero-warning Release build, publish, and
all 396 tests (143 app and 253 core) pass. The authored, published, and four native-QA
pack copies are byte-identical with SHA-256
`9a665483e615b5bc368474d5d2175758b82b164beab5e2eb6701f1cc6e11bfeb`.

Fresh native macOS interaction used four isolated schema 7 profiles. English lessons
91 and 100, Hindi lesson 98, and Hinglish lesson 99 played end to end and returned to
the course map. The checks covered mouse and keyboard navigation, scene replay and
skip, deterministic failure and recovery, ordered listening with full transcripts,
matching, exact form entry, notes, album navigation, a four-step capstone, typed
microphone-free speech, recap completion, and all three authored instruction routes.
Light and dark themes were inspected. Reduced-motion profiles exposed instant final
states, while the motion-enabled dark profile responded to replay and skip. The
macOS accessibility tree exposed named controls, text equivalents, status text, and
live outcome regions. Preview visits left curriculum, task, pronunciation, review,
and lesson-history collections empty.

Native QA found two defects and verified their corrections. Recap acknowledgements
used the visible `Fertig` label instead of the deterministic `finish` action ID, which
raised an argument exception on the first English completion; all 10 recaps now use
the valid action ID and a regression test exercises every mapping. Four-member scene
casts extended beyond the stage in dark mode; crowded casts now use a centered,
narrower paper-card layout with a renderer regression test and a post-fix native
screenshot. A separate English QA launch produced a macOS application-registration
abort report before content loaded; it did not recur in subsequent launches, but its
environmental root cause is not established.

Named evidence gaps: direct VoiceOver remains unverified. Windows native interaction
is intentionally deferred under the current macOS-only scope. A full end-to-end
native playthrough of all 10 lessons in each declared instruction language remains
unverified; current evidence combines complete representative playthroughs,
deterministic unit tests, and complete validator coverage. Catalog-owned fixed labels
and status copy remain English in some Hindi- and Hinglish-taught renderers; authored
instructions and live outcomes switch correctly, and full renderer-chrome
localization remains the named P5.4 follow-up before content approval. Competent
German, Hindi, Hinglish, pedagogical, cultural, content, license, modification, and
redistribution review remains pending. No Unit 10 lesson is described as approved.
The authored course now contains 100 lessons and 799 deterministic template
instances. The exact next Phase 7 step is P7.B11, Unit 11's 10 review-gated Health and
appointments lessons.

**Unit 11 batch status (2026-09-06): complete with named unverified evidence.**
P7.B11 is authored directly without local-model lesson generation. The batch contains
10 ordered A2 lessons and 80 unique deterministic template instances across 25 catalog
kinds, with 10 concepts, 32 lexicon entries, one deterministic reception-relay task,
10 error rules, 10 feedback templates, one rubric, and 10 unscored pronunciation text
scripts. Every lesson opens with Scene Establish, ends with Recap Scrapbook, and
remains machine-validated Preview content. English, Hindi, and authored Latin-script
Hinglish maps cover learner-facing instructions while German examples, answer IDs,
ordering, outcomes, and the dependency chain from Unit 10 remain deterministic
single-copy data.

The batch uses no asset references. A reviewed body-map cutout set, fictional
practice-label card, and reception-counter backdrop remain named asset follow-ups;
every affected surface renders a complete authored text equivalent instead of a
placeholder or network resource. Health content is limited to fictional routine
reception communication. It does not diagnose, triage, recommend treatment, or save
learner health data. Practice labels visibly state `KEIN ECHTES MEDIKAMENT` and
`NICHT EINNEHMEN`. Speech practice is typed, microphone-free, and unscored. Ten
claim-level source records cite the CEFR Companion Volume, Goethe-Institut A2 exam and
vocabulary materials, IDS Grammis, and German federal health information. Competent
review and all license gates remain in force.

JSON parsing, stable 101-to-110 lesson ordering, instance and template-kind counts,
complete three-language instruction maps, machine-validation status, no-Devanagari
Hinglish, safety-warning presence, no-asset policy, deterministic outcome mapping,
and byte-stable regeneration checks pass. Locked restore, formatter verification, a
zero-warning Release build, publish, and all 399 tests (145 app and 254 core) pass.
The authored, published, and four native-QA pack copies are byte-identical with
SHA-256 `9f9057365f65c3b0ad7a300e67bf3e083cc35e3fff01dedb8c1eecd85deeec98`.

Fresh native macOS interaction used four isolated schema 7 profiles with the
microphone set to Never. Reduced-motion English lesson 101 played end to end and
returned to the 110-lesson course map. The run covered mouse and keyboard navigation,
all four album pages, pair matching, deterministic sorting, an audio-led choice with
its full written transcript, exact typed microphone-free speech, recap completion,
named controls, text equivalents, and live outcome regions. Reduced-motion Hindi
lesson 108 played end to end through the complete fictional pharmacy-label warning,
unsafe-choice failure and recovery, schedule recovery, form fill, recap, and map
return. Reduced-motion Hinglish lesson 109 played end to end in Latin script through
the written microphone-free route, failure and recovery, form fill, no-diagnosis
relay, typed prompt, note writing, recap, and map return. Motion-enabled dark-theme
lesson 110 played end to end with mouse and keyboard, replay, a skip issued during
entrance motion, deterministic failure and recovery, ordered capstone steps, typed
microphone-free speech, recap replay, completion, and map return. Together the runs
cover both themes and reduced-motion instant and motion-enabled final states.

Native inspection found two renderer containment defects and closed both. Long Scene
Establish location tape now stays centered and bounded. Listen Route now computes a
contained three-column stage, keeps its instruction tape unobstructed, and wraps all
selected and available route labels. Regression tests cover both fixes; fresh dark
screenshots confirm the empty and fully selected route states without clipping or
overlap. Screenshots are retained as visual evidence only; the interaction claims
above come from the native playthroughs.

All four profiles retain empty curriculum progress and attempts, task attempts and
review handoffs, pronunciation attempts, review schedules and attempts, and lesson
history. Diagnostics contain only normal `appOpened` and successful `profileLoaded`
events. No macOS crash report was created during this verification run; the newest
Linguistics report is timestamped 2026-09-06 00:42:53 +0100 and predates the run.

Direct VoiceOver remains unverified. Windows native interaction is intentionally
deferred under the current macOS-only scope. Full end-to-end native playthroughs of
all ten lessons in every declared instruction language remain unverified.
Catalog-owned fixed labels and status copy remain English in some Hindi- and
Hinglish-taught renderers; full renderer-chrome localization remains the named P5.4
follow-up. Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No Unit 11 lesson is
described as approved. The authored course currently contains 110 lessons and 879
deterministic template instances. The exact next Phase 7 step is P7.B12, Unit 12's 10
review-gated Shopping and choices lessons.

**Unit 12 batch status (2026-09-06): complete with named unverified evidence.**
P7.B12 is authored directly by Codex without local- or remote-model lesson generation.
The batch contains 10 ordered A2 lessons and 80 unique deterministic template instances
across 30 catalog kinds, with 10 concepts, 32 lexicon entries, one deterministic
shopping task, 10 error rules, 10 feedback templates, one rubric, and 10 unscored
pronunciation text scripts. Every lesson opens with Scene Establish, ends with Recap
Scrapbook, and remains machine-validated Preview content. English, Hindi, and authored
Latin-script Hinglish maps cover learner-facing instructions while German examples,
answer IDs, ordering, outcomes, and the dependency chain from Unit 11 remain
deterministic single-copy data.

The batch uses no asset references. Reviewed storefront and clothing cutouts, product
information cards, and a fictional receipt remain named asset follow-ups; every affected
surface renders its complete authored text equivalent instead of a placeholder or
network resource. Speech practice is typed, microphone-free, and unscored. Ten
claim-level source records cite the CEFR Companion Volume, Goethe-Institut A2 exam and
vocabulary materials, IDS Grammis, Verbraucherzentrale, and German federal consumer
information. Competent review and all license gates remain in force.

JSON parsing, stable 111-to-120 lesson ordering, instance and template-kind counts,
complete three-language instruction maps, machine-validation status, no-Devanagari
Hinglish, no-asset policy, deterministic outcome mapping, and byte-stable regeneration
checks pass. Locked restore, formatter verification, a zero-warning Release build,
publish, and all 400 tests (145 app and 255 core) pass. The authored, published, and
four native-QA pack copies are byte-identical with SHA-256
`7eae4be271bf48ee8fdec6aeeadc50052da5ce4602a9c441e7c62fbb2e975c7e`.

Fresh native macOS interaction used four isolated schema 7 profiles with the microphone
set to Never. Reduced-motion English lesson 111 played end to end through album paging,
failure and recovery, article choice, plural reveal, basket reassignment, exact typed
microphone-free speech, recap replay, completion, and return to the 120-lesson course
map. Reduced-motion Hindi lesson 118 played end to end through the complete authored
product label, material-choice recovery, form fill, written listening alternative,
note writing, recap, and map return. Reduced-motion Hinglish lesson 119 played end to
end in Latin script through both postcard sides, ordered relay construction, scenario
failure and recovery, form fill, exact typed microphone-free speech, note writing,
recap, and map return. Motion-enabled dark-theme lesson 120 played end to end with
replay, a skip during entrance motion, deterministic failure and recovery, a written
listening route, ordered capstone steps, exact typed microphone-free speech, recap
replay, completion, and map return. Together the runs cover both themes, mouse and
keyboard operation, reduced-motion instant states, and motion-enabled final states.
No Unit 12 renderer defect was observed, and the Unit 11 scene-label containment fix
held with the longer shopping location tape.

All four profiles retain empty curriculum progress and attempts, task attempts and
review handoffs, pronunciation attempts, review schedules and attempts, and lesson
history. Diagnostics contain only normal `appOpened` and successful `profileLoaded`
events. No crash report names a Unit 12 QA bundle. One report appeared during the wider
verification window for the stale Unit 11 Hinglish QA process launched before Unit 12;
it records an unsymbolicated managed-exception `SIGABRT`, and its cause remains a named
unverified follow-up rather than evidence about a Unit 12 playthrough.

Direct VoiceOver remains unverified. Windows native interaction is intentionally
deferred under the current macOS-only scope. Full end-to-end native playthroughs of all
ten lessons in every declared instruction language remain unverified. Catalog-owned
fixed labels and status copy remain English in some Hindi- and Hinglish-taught
renderers; full renderer-chrome localization remains the named P5.4 follow-up.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No Unit 12 lesson is
described as approved. The authored course currently contains 120 lessons and 959
deterministic template instances. The exact next Phase 7 step is P7.B13, Unit 13's 10
review-gated Travel and accommodation lessons.

**Unit 13 batch status (2026-09-06): complete with named unverified evidence.**
P7.B13 is authored directly by Codex without local- or remote-model lesson generation.
The batch contains 10 ordered A2 lessons and 80 unique deterministic template instances
across 29 catalog kinds, with 10 concepts, 32 lexicon entries, one deterministic travel
and accommodation task, 10 error rules, 10 feedback templates, one rubric, and 10
unscored pronunciation text scripts. Every lesson opens with Scene Establish, ends with
Recap Scrapbook, and remains machine-validated Preview content. English, Hindi, and
authored Latin-script Hinglish maps cover learner-facing instructions while German
examples, answer IDs, ordering, outcomes, and the dependency chain from Unit 12 remain
deterministic single-copy data.

The batch uses no asset references. Reviewed station, vehicle, hotel-room,
booking-confirmation, and service-desk art remain named asset follow-ups; every affected
surface renders its complete authored text equivalent instead of a placeholder or
network resource. Speech practice is typed, microphone-free, and unscored. Eleven
claim-level source records cite the CEFR Companion Volume, Goethe-Institut A2 exam,
vocabulary, phrase, and glossary materials, IDS Grammis, Deutsche Bahn passenger-rights
information, and Verbraucherzentrale travel-booking guidance. Competent review and all
license gates remain in force.

JSON parsing, stable 121-to-130 lesson ordering, instance and template-kind counts,
complete three-language instruction maps, machine-validation status, no-Devanagari
Hinglish, no-asset policy, deterministic outcome mapping, and byte-stable regeneration
checks pass. Locked restore, formatter verification, a zero-warning Release build,
publish, and all 402 tests (146 app and 256 core) pass. The authored, published, and four
native-QA pack copies are byte-identical with SHA-256
`a70afc466fd9bdcb36805d26f8568b72a64c3c7fc98950137d795e95684c08cc`. The four final
native-QA `Linguistics-bin` executables are also byte-identical with SHA-256
`1b3a151e64a3d11adc13a046d6119b23fcefb097b3550bbb424407a36ed1be24`.

Fresh native macOS interaction used four isolated schema 7 profiles with the microphone
set to Never. Reduced-motion light-theme English lesson 121 played end to end through
text-only album paging, schedule and gap failure and recovery, sentence unfolding,
basket reassignment, exact typed microphone-free speech, recap replay, completion, and
map return. Reduced-motion Hindi lesson 128 played end to end on the final binary through
the complete seven-line fictional booking sign, a designed text-only unavailable state,
price and date failure and recovery, form fill, schedule reading, word and pair matching,
note writing, recap, and map return. Reduced-motion Hinglish lesson 129 played end to end
in Latin script on the final binary through both postcard sides, an ordered written
listening route, scenario failure and recovery, form fill, exact typed microphone-free
speech, note writing, recap, and map return. Motion-enabled dark-theme lesson 130 played
end to end on the final binary with replay, a skip during entrance motion, dialogue
comprehension, route construction, deterministic failure and recovery, form fill,
ordered capstone steps, exact typed microphone-free speech, recap replay, completion,
and map return. Together the runs cover both themes, mouse and keyboard operation,
reduced-motion instant states, and motion-enabled final states.

Native inspection exposed two shared-renderer defects: schedule headings were tied to an
appointment context, and long authored sign text could clip. Both were fixed, covered by
tests, and reverified in Hindi lesson 128 on the final binary. The full English lesson 121
run preceded those final renderer patches; all final binaries are byte-identical and the
changed renderer paths were exercised afterward, but a second complete English run on
the final binary remains unverified. During the first Hinglish route attempt, a batched
Tab and Return sequence moved focus into global navigation after a selected route button
became disabled. A controlled rerun completed through discrete accessible actions; exact
focus continuation after route selection remains a named keyboard follow-up.

All four profiles retain empty curriculum progress and attempts, task attempts and
review handoffs, pronunciation attempts, review schedules and attempts, and lesson
history. Diagnostics contain only normal `appOpened` and successful `profileLoaded`
events. No crash report names a Unit 13 QA bundle.

Direct VoiceOver remains unverified. Windows native interaction is intentionally
deferred under the current macOS-only scope. Full end-to-end native playthroughs of all
ten lessons in every declared instruction language remain unverified. Catalog-owned
fixed labels and status copy remain English in some Hindi- and Hinglish-taught
renderers; full renderer-chrome localization remains the named P5.4 follow-up.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No Unit 13 lesson is
described as approved. The authored course currently contains 130 lessons and 1,039
deterministic template instances. The exact next Phase 7 step is P7.B14, Unit 14's 10
review-gated Work and responsibilities lessons.

**Unit 14 batch status (2026-09-06): complete with named unverified evidence.**
P7.B14 is authored directly by Codex without local- or remote-model lesson generation.
The batch contains 10 ordered A2 lessons and 80 unique deterministic template instances
across 28 catalog kinds, with 10 concepts, 32 lexicon entries, one deterministic work
and responsibilities task, 10 error rules, 10 feedback templates, one rubric, and 10
unscored pronunciation text scripts. Every lesson opens with Scene Establish, ends with
Recap Scrapbook, and remains machine-validated Preview content. English, Hindi, and
authored Latin-script Hinglish maps cover learner-facing instructions while German
examples, answer IDs, ordering, outcomes, and the dependency chain from Unit 13 remain
deterministic single-copy data.

The batch uses no asset references. Reviewed workplace, occupation-badge, task-board,
rota, and team-table art remain named asset follow-ups; every affected surface renders
its complete authored text equivalent instead of a placeholder or network resource.
All work situations are explicitly fictional and bounded, with no employment or legal
advice. Speech practice is typed, microphone-free, and unscored. Ten claim-level source
records cite the CEFR Companion Volume, Goethe-Institut A2 exam, vocabulary, Deutsch
Online, and modal-verb materials, plus IDS Grammis coverage of modal verbs, personal
pronouns, separable verbs, and noun compounds. Competent review and all license gates
remain in force.

JSON parsing, stable 131-to-140 lesson ordering, instance and template-kind counts,
complete three-language instruction maps, machine-validation status, no-Devanagari
Hinglish, no-asset policy, deterministic outcome mapping, and byte-stable regeneration
checks pass. Locked restore, formatter verification, a zero-warning Release build,
macOS publish, and all 403 tests (146 app and 257 core) pass. The authored, published,
and four native-QA pack copies are byte-identical with SHA-256
`34c1a0744462dd9c004aee98dcbee72bc51acacd23eaa97ee49d46543aa08610`. The four final
native-QA `Linguistics-bin` executables are also byte-identical with SHA-256
`1b3a151e64a3d11adc13a046d6119b23fcefb097b3550bbb424407a36ed1be24`.

Fresh native macOS interaction used four isolated schema 7 profiles with the microphone
set to Never and no local model selected. Reduced-motion light-theme English lesson 131
played end to end through scene replay and skip, text-only occupation album paging,
word and gap failure and recovery, sentence expansion, category reassignment, exact
typed microphone-free speech, recap replay, completion, and map return. Reduced-motion
Hindi lesson 138 played end to end through the complete fictional rota, sign failure and
recovery, schedule reading, five-field form fill, word matching, all four card pairs,
note writing, recap replay, completion, and map return. Reduced-motion Hinglish lesson
139 played end to end in Latin script through both postcard sides, the written audio
alternative, an ordered route, scenario failure and recovery, form fill, exact typed
microphone-free speech, note writing, recap replay, completion, and map return.
Motion-enabled dark-theme lesson 140 played end to end through scene replay and a skip
during entrance motion, the full dialogue transcript, dialogue and scenario failure and
recovery, route construction, form fill, ordered capstone steps, exact typed
microphone-free speech, recap replay, completion, and map return. Tab and Return activated
the opening-scene control and selected the first listening-route stop; the live
`LessonTemplateOutcome` region changed after the keyboard action. Together the runs
cover both themes, mouse and keyboard operation, reduced-motion instant states, and
motion-enabled final states.

All four profiles retain empty curriculum progress and attempts, task attempts and
review handoffs, pronunciation attempts, review schedules and attempts, and lesson
history. Diagnostics contain only normal `appOpened` and successful `profileLoaded`
events. The two existing macOS Linguistics crash reports predate every Unit 14 QA run;
no crash report was created during this batch's native interaction.

Direct VoiceOver remains unverified. Windows native interaction is intentionally
deferred under the current macOS-only scope. Full end-to-end native playthroughs of all
ten lessons in every declared instruction language remain unverified. Catalog-owned
fixed labels and status copy remain English in some Hindi- and Hinglish-taught
renderers; full renderer-chrome localization remains the named P5.4 follow-up.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No Unit 14 lesson is
described as approved. The authored course currently contains 140 lessons and 1,119
deterministic template instances. The exact next Phase 7 step is P7.B15, Unit 15's 10
review-gated Home services and repairs lessons.

**Unit 15 batch status (2026-09-06): complete with named unverified evidence.**
P7.B15 is authored directly by Codex without local- or remote-model lesson generation.
The batch contains 10 ordered A2 lessons and 80 unique deterministic template instances
across 28 catalog kinds, with 10 concepts, 32 lexicon entries, one deterministic home-
service task, 10 error rules, 10 feedback templates, one rubric, and 10 unscored
pronunciation text scripts. Every lesson opens with Scene Establish, ends with Recap
Scrapbook, and remains machine-validated Preview content. English, Hindi, and authored
Latin-script Hinglish maps cover learner-facing instructions while German examples,
answer IDs, ordering, outcomes, and the dependency chain from Unit 14 remain
deterministic single-copy data.

The batch uses no asset references. Reviewed home-interior, fault-icons, service-van,
repair-note, and technician-puppet art remain named asset follow-ups; every affected
surface renders its complete authored text equivalent instead of a placeholder or
network resource. Every service situation is explicitly fictional and bounded, with no
real repair, safety, legal, or pricing advice. Speech practice is typed, microphone-free,
and unscored. Twelve claim-level source records cite the CEFR Companion Volume,
Goethe-Institut A2 exam, vocabulary, and Deutsch Online materials, plus IDS Grammis
coverage of objects, dative objects, subordinate clauses, verb placement, commas, word
stress, and noun compounds. Competent review and all license gates remain in force.

JSON parsing, stable 141-to-150 lesson ordering, instance and template-kind counts,
complete three-language instruction maps, machine-validation status, no-Devanagari
Hinglish, no-asset policy, deterministic outcome mapping, and byte-stable regeneration
checks pass. Locked restore, formatter verification, a zero-warning Release build,
macOS publish, and all 405 tests (147 app and 258 core) pass. The authored, built,
published, and four native-QA pack copies are byte-identical with SHA-256
`b191d600e4fb7c11f4ea308cca54a0f54403d6c289acb27f725b62729c2f3381`. The Release
publish executable has SHA-256
`1b3a151e64a3d11adc13a046d6119b23fcefb097b3550bbb424407a36ed1be24`; the four
ad-hoc-signed native-QA `Linguistics-bin` executables are mutually byte-identical with
SHA-256 `8657524875443f9ca09b014e58f6855fc606ebc07e658365f1b94f6e047f0b50`, and all four
bundles pass deep strict code-signature verification.

Fresh native macOS interaction used four isolated schema 7 profiles with the microphone
set to Never and no local model selected. Reduced-motion light-theme English lesson 141
played end to end through scene replay and skip, all text-only album pages, word and gap
failure and recovery, category reassignment, all four card pairs, exact typed
microphone-free speech, recap replay, completion, and map return. This run exposed a
three-row Pair Cards clipping defect; the renderer now expands four-pair stages, a
regression test covers the required height, and the formerly hidden final pair was then
matched successfully in the refreshed app. Reduced-motion Hindi lesson 148 played end
to end through the service message, sign failure and recovery, schedule reading,
five-field form fill, word matching, note writing, exact typed microphone-free speech,
recap, completion, and map return. Reduced-motion Hinglish lesson 149 played end to end
in Latin script through both source-card sides, the complete written listening
alternative, ordered event and route construction, scenario failure and recovery,
sorting, note writing, recap outcome cycling, completion, and map return.

Motion-enabled dark-theme lesson 150 played end to end through scene replay and a skip
during entrance motion, the complete corrected dialogue and five-stage route transcript,
scenario failure and recovery, five-field form fill, all ordered capstone steps, exact
typed microphone-free speech, recap, completion, and map return. Tab and Return operated
the English opening-scene controls; Hindi form entry and submission also used the
keyboard. Named controls, complete text equivalents, status messages, and live
`LessonTemplateOutcome` regions were exposed in the macOS accessibility tree. Fresh
visual captures cover the repaired four-pair stage, Hindi recap, dark scenario, typed
speech route, and dark recap; screenshots remain visual evidence only, while the
interaction claims above come from native control activation.

All four profiles retain empty curriculum progress and attempts, task attempts and
review handoffs, pronunciation attempts, review schedules and attempts, and lesson
history. Diagnostics contain only normal `appOpened` and successful `profileLoaded`
events. The newest existing macOS Linguistics crash report is timestamped 2026-09-06
10:24:34 +0100 and predates every Unit 15 QA run; no crash report was created during
this batch's native interaction.

Direct VoiceOver remains unverified. Windows native interaction is intentionally
deferred under the current macOS-only scope. Full end-to-end native playthroughs of all
ten lessons in every declared instruction language remain unverified. Catalog-owned
fixed labels and status copy remain English in some Hindi- and Hinglish-taught
renderers; full renderer-chrome localization remains the named P5.4 follow-up.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No Unit 15 lesson is
described as approved. The authored course currently contains 150 lessons and 1,199
deterministic template instances. The exact next Phase 7 step is P7.B16, Unit 16's 10
review-gated Invitations and reasons lessons.

**Unit 16 batch status (2026-09-07): complete with named unverified evidence.**
P7.B16 is authored directly by Codex without local- or remote-model lesson generation.
The batch contains 10 ordered A2 lessons and 80 unique deterministic template instances
across 27 catalog kinds, with 10 concepts, 32 lexicon entries, one deterministic social-
planning task, 10 error rules, 10 feedback templates, one rubric, and 10 unscored
pronunciation text scripts. Every lesson opens with Scene Establish, ends with Recap
Scrapbook, and remains machine-validated Preview content. English, Hindi, and authored
Latin-script Hinglish maps cover learner-facing instructions while German examples,
answer IDs, ordering, outcomes, and the dependency chain from Unit 15 remain
deterministic single-copy data.

The batch uses no asset references. Reviewed leisure-icon, invitation-card, group-chat-
strip, event-calendar, and friend-puppet art remain named asset follow-ups; every
affected surface renders its complete authored text equivalent instead of a placeholder
or network resource. Every planning situation is explicitly fictional and bounded.
Speech practice is typed, microphone-free, and unscored. Twelve claim-level source
records cite the CEFR Companion Volume, Goethe-Institut A2 exam, vocabulary, and
Deutsch Online materials, plus IDS Grammis coverage of causal clauses, verb placement,
subordinate-clause commas, modal verbs, indirect questions, and word stress. Friendly
invitation intonation remains a named expert-review need. Competent review and all
license gates remain in force.

JSON parsing, stable 151-to-160 lesson ordering, instance and template-kind counts,
complete three-language instruction maps, machine-validation status, no-Devanagari
Hinglish, no-asset policy, deterministic outcome mapping, and byte-stable regeneration
checks pass. Locked restore, formatter verification, a zero-warning Release build,
macOS publish, and all 406 tests (147 app and 259 core) pass. The authored, published,
and four native-QA pack copies are byte-identical with SHA-256
`6c390d5aee0df4f48b2149f6e629b5a0972d19a3a9b621f4e7010ff988ea512a`. The Release
publish executable has SHA-256
`1b3a151e64a3d11adc13a046d6119b23fcefb097b3550bbb424407a36ed1be24`. The four signed
QA-bundle main executables have distinct post-signing hashes, so no executable byte-
identity claim is made; all four bundles pass deep strict code-signature verification.

Fresh native macOS interaction used four isolated schema 7 profiles with the microphone
set to Never and no local model selected. Reduced-motion light-theme English lesson 151
played end to end through scene replay and skip, every text-only album page, schedule,
dialogue, form, and word-order failure and recovery, exact typed microphone-free speech,
recap outcome cycling, completion, and map return. Reduced-motion Hindi lesson 158
played end to end through scene replay and skip, sign failure and recovery, schedule,
form, word matching, note writing, exact typed microphone-free speech, recap outcome
cycling, completion, and a localized non-mastery map status. Reduced-motion Hinglish
lesson 159 played end to end in Latin script through scene replay and skip, both
postcard sides, the complete written listening alternative, scenario failure and
recovery, sorting, note writing, recap outcome cycling, completion, and map return.

Motion-enabled dark-theme lesson 160 played end to end through keyboard map navigation,
scene replay, a captured intermediate entrance state, the settled final state within
3.5 seconds, and skip. Dialogue, route, scenario, five-field form, and ordered capstone
outcomes matched their deterministic mappings. Exact typed microphone-free speech
included the indirect time question, the microphone control remained disabled, and the
surface stated that pronunciation was not assessed. Recap Again and Done returned to a
non-mastery map status. Mouse and keyboard operation, both themes, reduced-motion
instant states, motion-enabled staging, replay, skip, and outcome cycling therefore have
native evidence. Accessibility names, complete text equivalents, status messages, and
live `LessonTemplateOutcome` regions were exposed in the macOS accessibility tree.
Fresh captures remain visual evidence only; interaction claims come from native control
activation.

All four profiles retain empty curriculum progress and attempts, task attempts and
review handoffs, pronunciation attempts, review schedules and attempts, and lesson
history. Diagnostics contain only normal `appOpened` and successful `profileLoaded`
events. No macOS Linguistics crash report was created during Unit 16 interaction. The
computer-control service itself produced one crash report during the automated dark-
theme run, recovered with the app state intact, and is not counted as application
stability evidence.

Direct VoiceOver remains unverified. Windows native interaction is intentionally
deferred under the current macOS-only scope. Full end-to-end native playthroughs of all
ten lessons in every declared instruction language and theme remain unverified.
Catalog-owned fixed labels and status copy remain English in some Hindi- and Hinglish-
taught renderers; full renderer-chrome localization remains the named P5.4 follow-up.
Competent German, Hindi, Hinglish, pedagogical, cultural, content, license,
modification, and redistribution review remains pending. No Unit 16 lesson is
described as approved. The authored course currently contains 160 lessons and 1,279
deterministic template instances. The exact next Phase 7 step is P7.B17, Unit 17's 10
review-gated Digital life and media lessons.

**Unit 17 batch status (2026-09-08): complete with named unverified evidence.**
P7.B17 is directly authored without a separate lesson-generation model. Digital life
and media contains lessons 161–170, 80 deterministic template instances across 12
catalog templates, 10 concepts, 16 lexical entries, one task, 10 error rules and
feedback templates, one rubric, and 10 unscored pronunciation scripts. English,
Hindi, and Latin-script Hinglish instruction maps are complete. Five-page albums
include the complete main and supporting examples. Typed rehearsal visibly supplies
the exact model wording; it does not claim free-response or pronunciation assessment.

Claim-level provenance includes CEFR, Goethe-Institut media/A2 references, and IDS
Grammis references for reflexives, comparison, pronoun order, verb position, modals,
causal clauses, and auxiliaries. All new content remains machine-validated Preview.
Competent German, Hindi, Hinglish, pedagogical, cultural, and license review remains
pending; no Unit 17 lesson is described as approved. No new image or audio asset was
fetched. Device cutouts, help screen, settings card, learner desk, and expert review
of borrowed-word pronunciation remain named follow-ups. Text equivalents are complete.

Locked restore, Release build with zero warnings/errors, full tests (147 app and
260 core), formatter verification, macOS publish, and whitespace checks pass.
Tests cover every authored activity's success/failure mapping, uncertain incomplete
form and speech responses, ordered capstone prefixes, reflexive choice, and optional
newsletter choice. Native authoring review found phrase chunks incorrectly occupying
the train's VERB 2 car, clipped long car labels, and an undisplayed rehearsal answer.
The final content corrects all three, with regression assertions for finite-verb
position, short car labels, five album pages, and visible model wording. The pack and
published QA copy share SHA-256
`b75bf424ff7a94da84bf753c324e506ce2763dd06f4b5c1e1f9a6245c88dc6ef`.

Fresh native macOS interaction on the final candidate completed English lesson 161,
Hindi lesson 168, and Hinglish lesson 169 end to end in dark theme with reduced motion.
This includes scene replay/skip, all five album pages, reading choices, dialogues,
forms, sentence trains, typed microphone-free comparison, recap outcome cycling,
completion, and map return. English reading failure/recovery was checked; Hinglish
also exercised the complete written listening transcript, event order, and mediation
note. Screenshots supply visual evidence only; mouse and keyboard control activation
and resulting accessibility-tree states supply interaction evidence. The isolated
schema 7 profile kept the microphone at Never, selected no model, and retains empty
curriculum, task, pronunciation, review, and lesson history after all three visits.

The computer-control service intermittently reported no-window, offscreen, capture,
and clipboard-timeout errors. Explicit window focus and keyboard navigation recovered
the tested routes; a timeout alone was never counted as a successful interaction.
Direct VoiceOver, motion-enabled and light-theme playback of this batch, full native
coverage of all ten lessons in all three languages, and native capstone playback
remain unverified. Windows interaction is deferred under the macOS-only scope.
P5.4's remaining English renderer chrome is still a named localization follow-up.
P4.T21 long-phrase car sizing remains a catalog follow-up; this batch uses complete
short labels that fit the existing renderer without changing scoring or persistence.

The authored course now contains 170 lessons and 1,359 template instances. The user
has requested all remaining lesson authoring, not a stop at this unit. The exact next
batch is P7.B18, Learning and development (171–180); Units 18–45 contain the remaining
280 unauthored lessons. This status does not mark Phase 7 complete.

**Unit 18 batch status (2026-09-08): complete with named unverified evidence.**
P7.B18 is directly authored without a separate lesson-generation model. Learning and
development contains lessons 171–180, 80 deterministic template instances, 10 concepts,
16 lexical entries, one task and rubric, 10 error/feedback pairs, and 10 unscored
pronunciation scripts. English, Hindi, and Latin-script Hinglish maps are complete.
Claim-level references include CEFR, Goethe learning strategies and A2 materials, and
IDS Grammis for infinitives, clause position, comparison, modals, and written stress
cues. Learning-desk, course-notice, and consultation-cutout assets remain named needs;
complete authored text is supplied without fetching media. All new content remains
machine-validated Preview, pending competent language, pedagogy, cultural, and rights
review. This does not extend the user's A1 approval to A2.

Locked restore, zero-warning Release build, full tests (147 app and 261 core),
formatter verification, and macOS publish pass. Tests cover deterministic activity
outcomes, incomplete answers, infinitive choice, A1 entry conditions, finite-verb
placement, readable short train labels, visible rehearsal wording, and the additional
written stress pages. The authored and published QA packs share SHA-256
`7199642833c83305538b6f19ad59b4485197010a6c42c889aaa7a28f798b1a98`.

Fresh native macOS interaction completed English lesson 171, Hindi lesson 173, and
Hinglish lesson 180 in light theme with reduced motion. Mouse and keyboard actions
exercised scene replay/skip, all album pages, reading failure and recovery, dialogue,
the zu gap, note writing, incomplete/complete forms, trains, typed microphone-free
rehearsal, recap cycling, and map return. The Hinglish capstone progressed through all
four ordered acknowledgements, with intermediate incomplete and final matched outcomes;
it does not execute a booking or assess an unconstrained conversation. Screenshots are
visual evidence only. The isolated schema 7 profile retains empty learning histories,
microphone Never, and no selected model after the three visits.

The computer-control service intermittently failed captures and offscreen actions;
fresh accessibility state and focused keyboard navigation recovered tested routes.
Capstone replay retained the completed acknowledgement chain, consistent with the
renderer’s entrance-only replay contract; a fresh-chain reset was not verified.
Direct VoiceOver, dark-theme and motion-enabled playback of this batch, full native
coverage of all ten lessons across all languages, and expert stress/audio review remain
unverified. Windows is deferred under macOS-only scope. P5.4 renderer-chrome localization
and P4.T21 long-label layout remain existing named follow-ups.

The authored course now contains 180 lessons and 1,439 template instances. The next
batch is P7.B19, A2 independence (181–190). Units 19–45 contain 270 remaining lessons.
The request to finish all authoring remains active; Phase 7 is not complete.

**Unit 19 batch status (2026-09-08): complete with named unverified evidence.**
P7.B19 is directly authored without a separate lesson-generation model. A2 independence
contains lessons 181–190, 80 deterministic template instances across 12 catalog kinds,
10 concepts, 16 lexical entries, one task and rubric, 10 error/feedback pairs, and 10
unscored pronunciation scripts. English, Hindi, and Latin-script Hinglish maps are
complete. CEFR and Goethe A2 references support the approximate communicative scope;
IDS Grammis sources support bounded perfect forms, subordinate clauses, modals, and
location versus destination. The movement example explicitly retains dative for a
walk within a garden. Optional breakfast is distinguished from prohibition. The
fictional travel mission preserves an unanswered hotel request, makes no real booking,
and explicitly does not certify A2 proficiency.

Locked restore, zero-warning Release build, all 409 tests (147 app and 262 core),
formatter, macOS publish, signature inspection of the local QA bundle, and whitespace
checks pass. Deterministic tests cover every activity's mapping and incomplete
responses, finite-verb placement, readable train labels, complete visible rehearsal
wording, ins in the destination gap, and the pending hotel answer. Native review found
clipped listening-event and speaker labels. The authored labels were shortened, with
regression assertions and fresh visual checks. No production renderer or scoring,
mastery, persistence, transfer, or speech-assessment code changed. Final authored and
published packs share SHA-256
`204bb307f357d45a0ae75d44a6cfc16b54afb9723d7212a61c9d3bf07ed825ec`.

Native dark-theme, reduced-motion macOS interaction completed English lesson 184,
Hindi lesson 187, and Hinglish lesson 190. Evidence includes mouse/keyboard controls,
scene replay/skip, five-page albums, reading failure/recovery, dialogue, modal gap,
the complete written listening exchange and ordered events, note writing, incomplete
and complete forms, ordered capstone acknowledgements, trains, typed rehearsal, recap
cycling, and map return. Hindi playback was repeated after shortening listening labels.
The final speaker-name-only correction was visually inspected and its typed comparison
retested on lesson 190; the full three-route run preceded that last label-only change.
Screenshots are visual evidence, not interaction proof. The isolated synthetic profile
kept microphone Never, no selected model, and empty learning histories throughout.

Capture failures, launcher timeouts, offscreen controls, and one conflicting paste
warning required fresh state checks and focused keyboard recovery. Unverified evidence
includes direct VoiceOver, light-theme and motion-enabled playback for this batch, all
ten lessons across all three languages, and a full three-route repetition after the
final label-only fix. Windows remains deferred. Travel-notice, station-cutout, and
hotel-message media remain named needs with complete authored text alternatives.
P5.4 renderer-chrome localization and the catalog's long-label sizing are existing
follow-ups. Competent German, Hindi, Hinglish, pedagogical, cultural, content, and rights
review remains pending; no A2 lesson is described as approved.

A1 and A2 authoring now spans 190 lessons and 1,519 template instances. A2's 100 lessons
are authored, not linguistically approved or certified. The next batch is P7.B20,
Tell a coherent story (B1 lessons 191–200). Units 20–45 contain 260 remaining lessons.
The user's all-authoring request remains active. Phase 7 is not complete.

**Unit 20 batch status (2026-09-08): complete with named unverified evidence.**
P7.B20 is directly authored without a separate lesson-generation model. Tell a
coherent story contains B1 lessons 191–200, 80 deterministic template instances across
12 catalog kinds, 10 concepts, 16 lexical entries, one task and rubric, 10
error/feedback pairs, and 10 unscored pronunciation scripts. English, Hindi, and
Latin-script Hinglish maps are complete. CEFR and Goethe B1 references support the
approximate narrative scope; IDS Grammis supports past forms, temporal clauses,
earlier-past formation, verb placement, and context-specific emphasis. Written aids
explain the earlier-past construction and suggest emphasis without claiming a
pronunciation score or general storytelling assessment.

Locked restore, zero-warning Release build, all 410 tests (147 app and 263 core),
formatter, macOS publish, strict local QA bundle signature inspection, and whitespace
checks pass. Mapping tests cover every activity, incomplete answers, finite-verb
placement, short train and listening labels, complete visible rehearsal models, the
als gap, and the story mission's Bibliothek ending. No production renderer, scoring,
mastery, persistence, transfer, or speech-assessment code changed. Authored and
published packs share SHA-256
`b51cd42b9b6761e0cbe170973193ac1f242c0c18c2fc694a4c932b0d2a439f00`.

Native light-theme, reduced-motion macOS interaction completed English lesson 194,
Hindi lesson 197, and Hinglish lesson 200. Evidence includes mouse and keyboard,
scene replay/skip, complete five- and six-page albums, wrong-answer recovery,
dialogue, nachdem gap, complete written narrative and four ordered events, a German
note, incomplete and complete mission forms, four ordered capstone acknowledgements,
trains, typed rehearsal, recap actions, and return to the course map. The mission
board acknowledges ordered steps; it does not assess free narration. Screenshots
show readable grammar/emphasis pages, event labels, and mission cards, but are visual
evidence only. The isolated synthetic profile retained microphone Never, no selected
model, and empty learning histories after these developer playback visits.

Intermittent macOS capture failures required fresh accessibility-state checks and
keyboard recovery; interrupted actions were not assumed successful. The repeated
long album navigation caption clips although the primary teaching text is readable:
name P4.T7 album-caption sizing as a follow-up. Direct VoiceOver, dark-theme and
motion-enabled interaction for this batch, and every lesson/language permutation
remain unverified. Windows remains deferred. rainy-park, library-reading-room, and
reader-cutouts are named asset needs with complete text alternatives, not fetched
media. P5.4 renderer-chrome localization remains a follow-up. Competent German,
Hindi, Hinglish, pedagogical, cultural, content, rights, and narrative-audio review
remains pending. These B1 lessons are machine-validated Preview, not approved.

The authored course now contains 200 lessons and 1,599 template instances. The next
batch is P7.B21, Opinions and reasons (201–210). Units 21–45 contain 250 remaining
lessons. The all-authoring request remains active; Phase 7 is not complete.

**Unit 21 batch status (2026-09-08): complete with named unverified evidence.**
P7.B21 is directly authored without a separate lesson-generation model. Opinions and
reasons contains lessons 201–210, 80 deterministic template instances across 12 catalog
kinds, 10 concepts, 16 lexical entries, one task and rubric, 10 error/feedback pairs,
and 10 unscored pronunciation scripts. English, Hindi, and Latin-script Hinglish maps
are complete. CEFR/Goethe B1 and discussion references support bounded opinion,
agreement, disagreement, and mediation practice. IDS Grammis supports denn/weil,
deshalb/darum, trotzdem/obwohl, verb placement, and a written contrastive-focus cue.
The fictional group's trial remains conditional on its unresolved price. Authored
examples distinguish opinions, reasons, examples, and evidence; no room is booked.

Locked restore, zero-warning Release build, all 412 tests (147 app and 265 core),
formatter, macOS publish, strict local QA signature inspection, and whitespace checks
pass. Initial validation caught a duplicate album-page ID; it was corrected before
the final green run. Tests cover every deterministic activity mapping, short labels,
six-page emphasis aid, complete visible typed models, written weil/obwohl clause
order, deshalb, unscored rehearsal, and the unresolved price. Static checks found no
em dashes or Devanagari in Hinglish maps and no translated copy block over 25 words.
Authored and published packs share SHA-256
`7786a5dad16b88a7a3b68358fcc0d27d6a2fb1d35b2dee2366a124ed91ca6bab`.

Native dark-theme, reduced-motion macOS interaction completed English lesson 206,
Hindi lesson 207, and Hinglish lesson 210. Evidence includes mouse/keyboard controls,
scene replay/skip, complete five- and six-page albums, reading failure/recovery,
dialogue, full written discussion and four ordered contributions, German notes,
incomplete/complete forms, ordered mission acknowledgements, trains, typed rehearsal,
recap actions, and map return. Screenshots visually document the emphasis aid,
discussion sequence, and Hinglish rehearsal; interaction is evidenced separately by
the control actions and resulting states. The synthetic profile retained microphone
Never, no selected model, and empty histories. Preview playback did not grant mastery.

The native launcher took about 30 minutes to return despite its requested timeout.
Repeated ScreenCaptureKit failures required state inspection and keyboard recovery;
no interrupted action was assumed to have succeeded. The P4.T7 repeated album caption
still clips; selected listening-card contrast in dark mode needs P8.2 measurement
and review. P5.4 renderer-chrome localization remains a follow-up. Direct VoiceOver,
light-theme/motion-enabled interaction for this batch, native gap-card practice, and
all ten lessons in all three languages remain unverified. Windows stays deferred.
study-room, park-discussion, and discussion-participant-cutouts remain named asset
needs with complete authored text alternatives. Competent German, Hindi, Hinglish,
pedagogical, cultural, content, rights, and contrastive-audio review remains pending.
These lessons are machine-validated Preview, not approved.

The authored course now contains 210 lessons and 1,679 template instances. The next
batch is P7.B22, Find and start work (211–220). Units 22–45 contain 240 remaining
lessons. The all-authoring request remains active; Phase 7 is not complete.

**Unit 22 batch status (2026-09-08): complete with named unverified evidence.**
P7.B22 is directly authored without a separate lesson-generation model. Find and start
work contains lessons 211–220, 80 deterministic template instances across 12 catalog
kinds, 10 concepts, 16 lexical entries, one task and rubric, 10 error/feedback pairs,
and 10 unscored pronunciation scripts. English, Hindi, and Latin-script Hinglish maps
are complete. CEFR/Goethe sources support approximate B1 work communication and
application structure. IDS Grammis supports relative clauses and clause-internal
case, bounded adjective endings, perfect auxiliaries, verb placement, and a written
workplace emphasis cue. The fictional vacancy, application, and interview are not
real submissions or offers. The separate fictional contract excerpt explicitly is
not legal advice, statutory minimums, net-pay calculation, or real-contract analysis.
Its terms are not silently imported into the vacancy's missing salary information.

Locked restore, zero-warning Release build, all 414 tests (147 app and 267 core),
formatter, macOS publish, strict isolated QA signature inspection, and whitespace
checks pass. Initial validation rejected a one-letter answer ID; it was corrected.
Native review then found the longer relative-clause activity clipped. A shorter
complete sentence, its own three-language example record, and a width regression
assertion now replace it. A test-analyzer complaint was corrected and the final
fail-fast build/test/format/publish sequence passed. Tests cover every activity's
deterministic mapping, den with Mira as subject, neue, fictional-contract disclaimer
and notice wording, deferred hiring, short labels, and the final Lebenslauf field.
Authored and published packs share SHA-256
`b137cbcba82422ad6bf3236ecb3d49863913c728af6f923814e7625a581af613`.

Native light-theme, reduced-motion macOS interaction completed English lesson 213,
Hindi lesson 218, and Hinglish lesson 220 on the final corrected candidate. Evidence
includes mouse navigation and extensive keyboard operation, scene replay/skip,
five-page albums, reading and dialogue comprehension, relative-gap failure/recovery,
four-field contract transcription, German note writing, incomplete/complete mission
forms, ordered capstone acknowledgements, sentence trains, typed rehearsal, recap
cycling, and map return. Fresh full-window screenshots show the shorter gap fitting,
the complete Hindi form, and the Hinglish model; malformed thumbnail captures were
not treated as layout evidence. Screenshots remain visual evidence only. The reused
isolated QA host loaded 220 lessons. Its synthetic profile kept microphone Never,
no selected model, and empty learning histories; no mastery was granted.

Intermittent capture failures and startup focus loss required fresh state checks;
interrupted actions were not presumed successful. P4.T22 long-sentence layout remains
a renderer follow-up, alongside P4.T7 repeated album captions and P5.4 chrome
localization. Direct VoiceOver, dark-theme/motion-enabled review for this batch,
native recruitment-listening and six-page emphasis-aid playback, and all lesson/
language permutations remain unverified. Windows stays deferred. office-notice,
interview-room, and recruiter-cutouts remain named asset needs with complete authored
text alternatives. Competent German, Hindi, Hinglish, pedagogical, cultural, content,
rights, and workplace-audio review remains pending. The new lessons are
machine-validated Preview, not approved.

The authored course now contains 220 lessons and 1,759 template instances. The next
batch is P7.B23, Housing and neighbours (221–230). Units 23–45 contain 230 remaining
lessons. The all-authoring request remains active; Phase 7 is not complete.

**Unit 23 batch status (2026-09-08): complete with named unverified evidence.**
P7.B23 is directly authored without a separate lesson-generation model. Housing and
neighbours contains lessons 221–230, 80 deterministic instances across 12 template
kinds, 10 concepts, 16 lexical entries, one task and rubric, 10 error/feedback pairs,
and 10 unscored pronunciation scripts. English, Hindi, and Latin-script Hinglish
maps are complete. CEFR/Goethe and IDS references support approximate B1 housing
communication, genitive relationships, relative attributes, conditional clauses,
verb placement, and a written schwa cue. Prices, house rules, and neighbour agreements
are fictional reading material, not legal advice. Listed costs leave electricity
unknown; a preference and neighbour agreement do not confirm a tenancy. The Saturday
house-party exception is not generalized to every evening.

Locked restore, zero-warning Release build, all 416 tests (147 app and 269 core),
formatter, macOS publish, strict QA signature inspection, and whitespace checks pass.
Final copy checks found no explanation block above 25 words after shortening one
Hindi translation. Tests exercise parameter validation and every activity's
deterministic outcomes, des/wenn/die, bounded labels, unresolved costs and lease,
and the scoped house-rule exception. Authored and published packs share SHA-256
`d3d6df44308c4b34153efc5a854883fa88b056219c482b4953d1a7a86281fea4`.

Native dark-theme, reduced-motion macOS interaction completed English lesson 224,
Hindi lesson 227, and Hinglish lesson 230. Mouse scene controls and extensive
keyboard operation exercised replay/skip, every page of the sampled albums,
reading/dialogue choices, wrong genitive and replay recovery, complete written
listening transcript and ordered events, note writing, incomplete/complete forms,
four ordered capstone acknowledgements, sentence trains, typed matching, recaps,
and course-map return. Capstone acknowledgements are not independent free-response
assessment. Fresh full-window screenshots show the fitting genitive sentence,
complete Hindi transcript, and completed mission. Screenshots are visual evidence
only. The isolated profile retained microphone Never, no model, and empty learning
histories. Typed matching explicitly did not assess pronunciation.

Capture failures required fresh state checks. Automation initially dropped an
umlaut in a note; exact pasted spelling passed. Direct VoiceOver, light-theme and
motion-enabled review, all 30 lesson/language combinations, and native playback of
the six-page schwa aid remain unverified. Windows stays deferred. P4.T7 repeated
album captions, P5.4 remaining renderer chrome localization, and P8.2 selected-card
contrast measurement remain follow-ups. apartment-listing, courtyard, and
neighbour-cutouts remain named asset needs with authored text alternatives.
Competent German, Hindi, Hinglish, pedagogy, cultural, content, rights, and reference
audio review remains pending. New content is machine-validated Preview, not approved.

The authored course now contains 230 lessons and 1,839 template instances. Next is
P7.B24, Health and wellbeing (231–240). Units 24–45 contain 220 remaining lessons.
The all-authoring request remains active; Phase 7 is not complete.

**Unit 24 batch status (2026-09-08): complete with named unverified evidence.**
P7.B24 is directly authored without a separate lesson-generation model. Health and
wellbeing contains lessons 231–240, 80 deterministic instances across 12 template
kinds, 10 concepts, 16 lexical entries, one task and rubric, 10 error/feedback pairs,
and 10 unscored pronunciation scripts. English, Hindi, and Latin-script Hinglish
maps are complete. CEFR/Goethe, IDS Grammis, and IQWiG references support approximate
B1 communication, reflexive/reciprocal reference, reported instructions, relative
clauses, cautious modal suggestions, speech grouping, and health-information literacy.
Fictional observations are not diagnoses; chronology is not proof of causation.
The abbreviated consultation explicitly is not a complete examination or advice to
wait. No personal health record, clinical triage, treatment, or recovery guarantee
is requested or supplied.

Locked restore, zero-warning Release build, all 418 tests (147 app and 271 core),
formatter, macOS publish, strict isolated QA signature inspection, and whitespace
checks pass. Initial validation caught an invalid concept enum label, which was
corrected before final gates. A short practice sentence was refined before final
publish. Copy checks cover explanation length, Hinglish script, and em-dash absence.
Tests cover every activity's deterministic mapping, mich/könntest/die, short labels,
the missing study, uncertain cause, no-waiting-advice boundary, and absent recovery
guarantee. Authored and published packs share SHA-256
`f571b4f81e5c9af06c323581a53b95222af0fcfa4078b43dca077f6a3b792ab6`.

Native light-theme, reduced-motion macOS interaction completed English lesson 233,
Hindi lesson 237, and Hinglish lesson 240. Evidence includes mouse scene controls,
keyboard navigation, replay/skip, every page of the sampled albums, reading and
dialogue choices, wrong reflexive selection and replay recovery, full written
consultation and ordered events, German notes, incomplete/complete forms, ordered
capstone acknowledgements, sentence trains, typed model matching, recaps, and map
return. Capstone acknowledgements do not independently assess clinical decisions.
Fresh screenshots show the fitting reflexive sentence, complete Hindi transcript,
and completed mission; screenshots are visual evidence only. Native capture failures
and one disconnected control pipe required fresh state inspection. An interrupted
action was never assumed complete; the final map return was independently observed.
The synthetic profile retained microphone Never, no model, and empty histories.

Direct VoiceOver, dark-theme/motion-enabled review, all 30 lesson/language combinations,
native six-page speech-grouping playback, and reference audio remain unverified.
Windows stays deferred. P8.2 selected-card contrast measurement remains a visible
follow-up, alongside P4.T7 repeated album captions and P5.4 renderer chrome
localization. consultation-room, observation-notebook, and clinician-cutouts remain
named asset needs with authored text alternatives. Competent German, Hindi, Hinglish,
pedagogy, medical-safety, cultural, content, and rights review remains pending. New
lessons remain machine-validated Preview, not approved.

The authored course contains 240 lessons and 1,919 template instances. Next is P7.B25,
Travel under pressure (241–250). Units 25–45 contain 210 remaining lessons. The
all-authoring request remains active; Phase 7 is not complete.

**Unit 25 batch status (2026-09-08): complete with named unverified evidence.**
P7.B25 is directly authored without a separate lesson-generation model. Travel under
pressure contains lessons 241–250, 80 deterministic instances across 12 template
kinds, 10 concepts, 16 lexical entries, one task and rubric, 10 error/feedback pairs,
and 10 unscored pronunciation scripts. English, Hindi, and Latin-script Hinglish
maps are complete. Nine claim-level CEFR/Goethe, IDS Grammis, and Deutsche Bahn
references support approximate B1 communication, embedded questions, passive notices,
polite requests, correction prominence, and process vocabulary. No source exercises
are copied. Fictional ticket validity, claim receipt, claim decision, and estimated
arrival are kept distinct. No real booking, submission, payment, current entitlement
threshold, or legal advice is supplied.

Locked restore, zero-warning Release build, all 420 tests (147 app and 273 core),
formatter, macOS publish, strict isolated QA signature inspection, and whitespace
checks pass. Tests cover every activity's deterministic mapping, ob/wird, short train
labels, final announcement correction, platform 4, the pending claim, and estimated
arrival. Authored and published packs share SHA-256
`96983f936e9b6f2a2dac3de43922428d2952a4afb3188400311a544529bbf91b`.

Native dark-theme, reduced-motion macOS interaction completed English lesson 243,
Hindi lesson 247, and Hinglish lesson 250. Evidence includes mouse scene replay/skip,
keyboard navigation, every page of the sampled albums, reading/dialogue answers,
wrong passive selection and replay recovery, the complete four-announcement written
prompt and ordered events, corrected German notes, incomplete/complete forms,
ordered capstone acknowledgements, trains, typed model matching, recaps, and map
return. An offscreen reading-page action was detected; the album was revisited and
every page completed before continuing. Clipboard timeouts were followed by direct
field inspection, never assumed to prove input. Capstone acknowledgements are not
independent assessment of real travel decisions. Fresh screenshots show the fitting
passive gap, full Hindi transcript, and mission board; these are visual evidence only.

Direct VoiceOver, light-theme/motion-enabled review, all 30 lesson/language
combinations, native six-page correction-prominence review, and reference audio
remain unverified. Windows stays deferred. P8.2 selected-card contrast measurement,
P4.T7 repeated album captions, and P5.4 renderer chrome localization remain named
follow-ups. station-concourse, departure-board, and service-desk-cutouts remain asset
needs with authored text alternatives using the existing paper-stage vocabulary.
Competent German, Hindi, Hinglish, pedagogy, cultural, content, and rights review
remains pending. New lessons remain machine-validated Preview, not approved.

The authored course contains 250 lessons and 1,999 template instances. Next is P7.B26,
Money and consumer choices (251–260). Units 26–45 contain 200 remaining lessons.
The all-authoring request remains active; Phase 7 is not complete.

**Unit 26 batch status (2026-09-08): complete with named unverified evidence.**
P7.B26 is directly authored without a separate lesson-generation model. Money and
consumer choices contains lessons 251–260, 80 deterministic instances across 12
template kinds, 10 concepts, 16 lexical entries, one task and rubric, 10 error/feedback
pairs, and 10 unscored pronunciation scripts. English, Hindi, and Latin-script
Hinglish maps are complete. Twelve claim-level CEFR/Goethe, IDS Grammis, Bundesbank,
Verbraucherzentrale, and BaFin/BAGSO references support approximate B1 communication,
payment-method vocabulary, compounds, comparison, passive processes, and protecting
credentials. The BaFin PDF's full fetch failed; indexed source evidence supports the
credential-boundary claim, but full document inspection remains unverified.
All amounts, dates, subscriptions, and case outcomes are fictional. Percentage
examples state their base and period, with simple-interest assumptions explicit.
An initiated refund is not visible credit; comparing offers is not buying one.
The lessons request no banking login, PIN, TAN, account number, or real transaction,
and give no personal financial recommendation or general legal entitlement.

Locked restore, zero-warning Release build, all 422 tests (147 app and 275 core),
formatter, macOS publish, strict isolated QA signature inspection, and whitespace
checks pass. An initial analyzer rejection of constant-only assertions was corrected
to check actual authored values against calculated results before the final gates.
Tests cover every activity's deterministic mapping, first-year totals including the
setup fee, the percentage base, interest assumptions, comparative/passive forms,
credential non-disclosure, pending credit, and no new contract. Copy checks find no
overlength explanation maps, Devanagari in Hinglish, or em dashes. Authored and
published packs share SHA-256
`ccb13d3a164f0b94b53d0839544c2a7e9c89782168aacc09ad6ebc786af84ac6`.

Native light-theme, reduced-motion macOS interaction completed English lesson 255,
Hindi lesson 257, and Hinglish lesson 260. Evidence includes mouse replay/skip,
keyboard navigation, every sampled album page, reading/dialogue answers, wrong
passive selection and replay recovery, full written banking exchange and ordered
events, German note retry, incomplete/complete forms, ordered capstone acknowledgements,
trains, typed model matching, recaps, and map return. The note checker rejected the
inflected wording schriftliche until the required word schriftlich was supplied;
this is a bounded token check, not free-form linguistic-quality assessment. Capstone
acknowledgements do not independently assess financial decisions. Fresh screenshots
show the passive gap, full Hindi transcript, and mission board. A launch capture
failure was recovered through fresh native state inspection. The synthetic profile
retained microphone Never, no model, and empty learning, task, speech, and review histories.

Direct VoiceOver, dark-theme/motion-enabled review, all 30 lesson/language combinations,
native six-page number-prominence review, and reference audio remain unverified.
Windows stays deferred. P8.2 selected-card contrast measurement, P4.T7 repeated
album captions, P5.4 renderer chrome localization, and note-check inflection tolerance
remain named follow-ups. account-statement-prop, service-counter, and subscription-cards
remain asset needs with authored text alternatives and existing paper-stage materials.
Competent German, Hindi, Hinglish, pedagogy, financial-safety, cultural, content, and
rights review remains pending. New lessons remain machine-validated Preview, not approved.

The authored course contains 260 lessons and 2,079 template instances. Next is P7.B27,
Education and lifelong learning (261–270). Units 27–45 contain 190 remaining lessons.
The all-authoring request remains active; Phase 7 is not complete.

**Unit 27 batch status (2026-09-08): complete with named unverified evidence.**

P7.B27 supplies lessons 261–270 with 80 deterministic instances across 12 template
kinds, 10 concepts, 16 lexemes, 10 feedback/error pairs, 10 microphone-free wording
models, and one bounded task/rubric. English, Hindi, and Hinglish explanations were
directly authored. Ten cited sources support the approximate B1 scope, purpose
constructions, noun capitalization, educational vocabulary, and presentation emphasis.
The lessons distinguish vocational and academic routes, same-person um … zu,
damit (also possible with the same person), nominalized activities, advisory questions,
course requirements, modules, and responsible mediation. Fictional admission and
qualification recognition remain open; no application is submitted or certificate awarded.

Locked restore, zero-warning Release build, all 424 tests (147 app and 277 core),
formatter, macOS publish, strict isolated QA signature inspection, and whitespace
checks pass. Parameter/outcome tests cover the new instances and educational
boundaries, including the 24 clock-hour calculation and unconfirmed places. Copy
checks found no overlength explanation maps, Devanagari in Hinglish, or em dashes.
Authored and published packs share SHA-256
`eaa933f796b89084bd22ca3be8107d49cf66353e4615bc094c8fbf04c766ea83`.

Native dark-theme, reduced-motion macOS interaction completed English lesson 263,
Hindi lesson 267, and Hinglish lesson 270. Evidence includes mouse scene replay/skip,
keyboard navigation, every sampled album page, reading/dialogue answers, wrong
connector selection and replay recovery, full written course briefing and ordered
events, timetable note, incomplete/complete form, four ordered capstone acknowledgements,
word-order trains, typed model matching, recaps, and return to the map. Capstone
acknowledgements do not independently assess educational choices. Typed comparison
explicitly reports that pronunciation was not assessed. Fresh screenshots show the
purpose gap, Hindi written briefing, and Hinglish mission board. The synthetic profile
retained microphone Never, no model, and empty learning, task, speech, and review histories.

Direct VoiceOver, light-theme/motion-enabled review, all 30 lesson/language combinations,
native six-page presentation-emphasis review, and reference audio remain unverified.
Windows stays deferred. P8.2 selected-card contrast measurement, P4.T7 repeated album
captions, P5.4 renderer chrome localization, and note-check inflection tolerance remain
named follow-ups. evening-classroom, course-brochure, and adviser-cutouts remain asset
needs; authored text alternatives reuse the existing paper-stage materials. Competent
German, Hindi, Hinglish, pedagogy, cultural, content, and rights review remains pending.
New lessons remain machine-validated Preview, not approved.

The authored course contains 270 lessons and 2,159 template instances. Next is P7.B28,
News and information (271–280). Units 28–45 contain 180 remaining lessons.
The all-authoring request remains active; Phase 7 is not complete.

**Unit 28 batch status (2026-09-08): authoring complete with named unverified evidence; native checkpoint blocked by locked Mac.**

P7.B28 supplies lessons 271–280 with 80 deterministic instances across 12 template
kinds, 10 concepts, 16 lexemes, 10 feedback/error pairs, 10 microphone-free wording
models, and one bounded task/rubric. English, Hindi, and Hinglish explanations were
directly authored. Eight cited sources support approximate B1 scope, reporting care,
passive constructions, vocabulary, and optional delivery practice. All reports,
speakers, dates, and publications are fictional. Lessons cover event/publication dates,
explicit opinion markers, source phrases, affected passive subjects, shared-source
dependence, neutral summaries, a complete bulletin, ambiguous headlines, mediation,
and a bounded briefing. Repetition is not independent confirmation; a question is
not evidence; a planned discussion is not a construction decision. No real story is
published, source contacted, event verified, or journalistic competence certified.

Final locked restore, zero-warning Release build, all 426 tests (147 app and 279 core),
formatter, whitespace checks, macOS publish, and strict isolated QA signature inspection
pass. Initial checks caught an empty gap prefix, single-letter page identifiers, and
long activity labels; these were corrected before the successful full run. An initial
runtime-specific QA publish changed lock files; those packaging-only changes were
restored, and the final publish used the existing target without lock-file changes.
Tests cover each activity's deterministic outcomes, distinct dates, shared attribution,
passive agreement, missing access information, and unresolved construction decisions.
Copy checks found no overlength explanation maps, Devanagari in Hinglish, or em dashes.
Authored and published packs share SHA-256
`6385ada1b9dff00b37a19ffd3ddf0b4b242eb3f08b2ff4970c1f311de8fde5e9`.

Native light-theme, reduced-motion macOS interaction completed English lesson 274
and Hindi lesson 277 end to end. Evidence includes mouse scene replay/skip, keyboard
navigation, every sampled album page, reading/dialogue answers, incorrect passive
agreement and replay recovery, the complete written bulletin and event ordering,
the source-aware note, trains, typed matching, recaps, and return to the map. Typed
comparison explicitly reported that pronunciation was not assessed. Hinglish lesson
280 reached the opening, all five album pages, source reading, and incomplete/complete
form checks. A capture error interrupted the capstone attempt. Fresh inspection after
returning from card 6 showed card 5 at its initial state, with step 1 available and
steps 2–4 waiting. The subsequent attempt was stopped because the Mac was locked and
automatic unlock was paused after physical input. Do not count the capstone, train,
typed response, recap, or map return as completed for this Hinglish lesson.

Fresh screenshots show the English passive gap, Hindi written bulletin, and initial
Hinglish mission board. The synthetic profile retains microphone Never, no selected
model, and empty learning, task, speech, and review histories. Direct VoiceOver,
dark-theme/motion-enabled review, all 30 lesson/language combinations, native six-page
neutral-delivery review, and reference audio remain unverified. Windows stays deferred.
P8.2 selected-card contrast measurement, P4.T7 repeated album captions, P5.4 renderer
chrome localization, and note-check inflection tolerance remain named follow-ups.
newsroom-desk, newspaper-props, and community-book-exchange remain asset needs with
authored text alternatives using existing paper-stage materials. Competent German,
Hindi, Hinglish, pedagogy, cultural, content, and rights review remains pending.
New lessons remain machine-validated Preview, not approved.

The course now contains 280 authored lessons and 2,239 template instances. Units 29–45
contain 170 remaining lessons. Exact resume step: manually unlock the Mac, complete
Hinglish lesson 280 from its four capstone acknowledgements through train, typed model,
recap, and map return, then author P7.B29 Society and culture (281–290). The all-authoring
request is not complete. GitHub CI for the preceding Unit 25–27 commits was inspected
and passed; this checkpoint's remote CI must be checked after pushing.

**Unit 28 native resume status (2026-09-08): locked-Mac evidence gap closed.**

After manual unlock, Hinglish lesson 280 completed all four ordered capstone
acknowledgements, its six-part word-order train, exact typed model comparison, recap,
and map return. The map explicitly reported that Preview did not change mastery;
typed comparison explicitly reported that pronunciation was not assessed. Intermittent
capture failures were recovered by fresh accessibility-state inspection and restored
keyboard focus; no failed capture was treated as evidence of a completed action.
The synthetic profile still has microphone Never, no model, and empty learning, task,
speech, and review histories. No production files or content changed during this resume.
GitHub CI passed for exact checkpoint `2251d2f857adeefbc8aa8f43698ab426e19c0fad`
(run `34285569618`). The other Unit 28 review gaps above remain unchanged. Next is
P7.B29 Society and culture (281–290); 170 planned lessons remain unauthored.

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
