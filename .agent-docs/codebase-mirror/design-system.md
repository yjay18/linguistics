# Design system and motion

## Repository paths

- `src/Linguistics.App/App.axaml`
- `src/Linguistics.App/MotionPreferences.cs`
- `src/Linguistics.App/Motion/`
- `src/Linguistics.App/Controls/`
- `src/Linguistics.App/Features/Developer/PaperStageSandboxView.axaml`
- `src/Linguistics.App/Features/Learn/Templates/`
- `src/Linguistics.App/Assets/PaperStage/`
- `src/Linguistics.App/MainWindow.axaml`
- `src/Linguistics.App/Features/Shell/`
- `src/Linguistics.App/Features/*/*.axaml`
- `tests/Linguistics.App.Tests/DesignSystemTests.cs`
- `tests/Linguistics.App.Tests/PaperMotionTests.cs`
- `tests/Linguistics.App.Tests/PaperStageTests.cs`
- `tests/Linguistics.App.Tests/TemplateGalleryViewTests.cs`
- `tests/Linguistics.App.Tests/TemplateRegistryTests.cs`

## Responsibility

`App.axaml` owns paired light/dark palette tokens, paper grain, tinted shadows, typography roles, semantic controls, navigation, focus styling, motion selectors, and the theme-safe ink used on light cutout-paper surfaces. `PaperCard`, `CutoutFrame`, `PaperTape`, `PaperStamp`, and `TornEdge` expose the material vocabulary without duplicating theme resources. `PaperStage` owns the fixed nine-layer scene order and puppet anchors. `SteppedEasing` and `PaperChoreography` own bounded presentation sequencing, replay, and skip-to-final behavior. The first three lesson-template renderers compose those primitives for scene, recognition, and construction interactions. Main window and shell own branded composition and page transitions. Feature XAML selects semantic classes but does not recreate tokens. `MotionPreferences` combines the saved learner choice with the explicit pre-profile environment override.

## Invariants

- Motion is presentation-only and never carries unique meaning or delays a state mutation.
- Reduced motion removes page crossfades, lifts, animated navigation, conversation entry, and paper choreography while retaining the complete final state plus all text, focus, color, layout, and live-region cues.
- Paper choreography remains under four seconds and supports an immediate skip to final values.
- Lesson-template choreography exposes replay and skip, settles in under two seconds for the first three renderers, and reaches the same complete state immediately under reduced motion.
- PaperStage order is backdrop, paper wash, supporting cast, ambient pieces, taped label, foreground silhouettes, subject, reaction burst, then verdict.
- The app uses native system typography and Avalonia/BCL primitives; there is no custom font or new UI/animation dependency.
- The developer PaperStage scene uses raster PNG cutouts with alpha rather than SVG/XAML vector scene art. Native vector paths remain limited to small paper UI decorations.
- Light and dark principal text, muted text, action, navigation, danger, focus, paper edge, tape, stamp, torn-edge, cutout, and nav-selection pairs meet their automated thresholds.
- Feature screens use the same semantic card/button language; danger and review-gated surfaces remain distinct.

## Checks

Release XAML compilation covers every view. App tests parse the actual theme dictionaries, calculate relative luminance/contrast for both themes, reject pure-black paper shadows, verify paired resources including cutout ink, and prove the sandbox contains raster scene images rather than vector scene art. Motion tests cover step counts, timeline bounds, reduced-motion completion, and synchronous skip. Stage tests cover the exact layer order, z-indexes, anchors, and offsets. Gallery and registry tests prove the developer route, exact three registrations, synthetic-fixture isolation, state cycling, and text-only switch. Persistence tests prove the additive reduced-motion field defaults off in earlier schema-5 files without rewriting them. Native minimum-window, scalable-text, keyboard, light/dark, motion, VoiceOver, and Narrator journeys remain separate final-candidate evidence.
