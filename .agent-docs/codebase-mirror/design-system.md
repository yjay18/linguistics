# Design system and motion

## Repository paths

- `src/Linguistics.App/App.axaml`
- `src/Linguistics.App/MotionPreferences.cs`
- `src/Linguistics.App/Motion/`
- `src/Linguistics.App/Controls/`
- `src/Linguistics.App/Features/Developer/PaperStageSandboxView.axaml`
- `src/Linguistics.App/Features/Learn/Templates/`
- `src/Linguistics.App/Content/ContentImageCache.cs`
- `content/languages/de/assets.json`
- `content/languages/de/assets/`
- `src/Linguistics.App/MainWindow.axaml`
- `src/Linguistics.App/Features/Shell/`
- `src/Linguistics.App/Features/*/*.axaml`
- `tests/Linguistics.App.Tests/DesignSystemTests.cs`
- `tests/Linguistics.App.Tests/PaperMotionTests.cs`
- `tests/Linguistics.App.Tests/PaperStageTests.cs`
- `tests/Linguistics.App.Tests/TemplateGalleryViewTests.cs`
- `tests/Linguistics.App.Tests/TemplateRegistryTests.cs`

## Responsibility

`App.axaml` owns paired light/dark palette tokens, paper grain, tinted shadows, typography roles, semantic controls, navigation, focus styling, motion selectors, and the theme-safe ink used on light cutout-paper surfaces. `PaperCard`, `CutoutFrame`, `PaperTape`, `PaperStamp`, and `TornEdge` expose the material vocabulary without duplicating theme resources. `PaperStage` owns the fixed nine-layer scene order and puppet anchors. `SteppedEasing` and `PaperChoreography` own bounded presentation sequencing, replay, and skip-to-final behavior. The 58 registered lesson-template renderers compose those primitives across the complete Wave A presentation, Wave B recognition, Wave C construction, Wave D listening, Wave E speaking, Wave F reading and writing, Wave G transfer and explanation, and Wave H scenario, review, and progress families. `TransferNoteCardView` supplies the reusable taped-note composition used by both the gallery and café scenario without owning routing or state. The developer PaperStage resolves its five generated layers through `ContentImageCache` and validated pack records rather than app-resource magic paths. Main window and shell own branded composition and page transitions. Feature XAML selects semantic classes but does not recreate tokens. `MotionPreferences` combines the saved learner choice with the explicit pre-profile environment override and supplies a zero-duration page transition when reduction is active so runtime preference changes cannot strand the transition presenter at partial opacity.

## Invariants

- Motion is presentation-only and never carries unique meaning or delays a state mutation.
- Reduced motion removes page crossfades, lifts, animated navigation, conversation entry, and paper choreography while retaining the complete final state plus all text, focus, color, layout, and live-region cues.
- Paper choreography remains under four seconds and supports an immediate skip to final values.
- Lesson-template choreography exposes replay and skip, remains below four seconds, and reaches the same complete state immediately under reduced motion.
- PaperStage order is backdrop, paper wash, supporting cast, ambient pieces, taped label, foreground silhouettes, subject, reaction burst, then verdict.
- The app uses native system typography and Avalonia/BCL primitives; there is no custom font or new UI/animation dependency.
- The developer PaperStage scene uses validated local pack raster assets, including alpha PNG cutouts, rather than SVG/XAML vector scene art or untracked app-resource paths. Native vector paths remain limited to small paper UI decorations.
- Light and dark principal text, muted text, action, navigation, danger, focus, paper edge, tape, stamp, torn-edge, cutout, and nav-selection pairs meet their automated thresholds.
- Feature screens use the same semantic card/button language; danger and review-gated surfaces remain distinct.

## Checks

Release XAML compilation covers every view. App tests parse the actual theme dictionaries, calculate relative luminance/contrast for both themes, reject pure-black paper shadows, verify paired resources including cutout ink, and prove the sandbox contains cache-assigned raster scene images rather than vector scene art. Motion tests cover step counts, timeline bounds, reduced-motion completion, synchronous skip, and zero-duration page transitions. Stage tests cover the exact layer order, z-indexes, anchors, and offsets. Gallery and registry tests prove the developer route, exact 58 registrations, one fixture per schema, provider-free rendering across every outcome and text-only state, state cycling, text-only punctuation hygiene, optional caption playback, typed speech routes, transient document input, routed transfer presentation, and deterministic timing, extraction, tile-order, advisory-action, review-rating, capstone-prefix, and capability-selection outcomes. Persistence tests prove the additive reduced-motion field defaults off in earlier schema-5 files without rewriting them. Native minimum-window, scalable-text, keyboard, light/dark, motion, VoiceOver, and Narrator journeys remain separate final-candidate evidence.

## Last reconciled

Phase 4 Wave H on 2026-09-02. Fresh macOS inspection covers every Wave H template,
complete text-only presentation, reduced-motion and motion-enabled final states, light
and dark themes, and real mouse and keyboard interaction on Scenario Theatre and Review
Flash. Native inspection and a first independent visual pass found scenario-label,
capstone-goal, and shelf-label readability defects. The corrected 10-capture review
passed every Wave H template and both interaction variants without regression. Direct
VoiceOver, Windows native interaction, real microphone capture, configured local
recognition, and a completed synthesized native drag gesture remain unverified.
