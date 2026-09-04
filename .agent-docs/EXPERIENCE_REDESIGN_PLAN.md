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
