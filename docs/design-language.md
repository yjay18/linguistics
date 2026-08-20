# Design language and motion

Linguistics uses a calm “local learning studio” language rather than game economy visuals. Warm paper-like canvases and rounded cards keep reading comfortable; deep forest navigation signals privacy and place; mint, amber, coral, and blue distinguish progress, attention, feedback, and evidence without becoming scores. Capability and next-action cards receive the strongest hierarchy. Developer, danger, and review-gated surfaces remain visibly distinct.

The design tokens live once in `App.axaml`, with matched light and dark theme resources. Typography styles define display, eyebrow, lede, section, and muted roles. Semantic card and button classes keep feature views small and consistent. No custom font, image asset, animation package, or UI dependency is required.

Motion is purposeful and short: navigation state changes and button feedback use 140 ms transitions, page changes use a 180 ms crossfade, and a new conversation turn enters over 180 ms. There is no looping reward animation. A saved “Reduce interface motion” preference removes those transitions while preserving color, focus, text, layout, and live-region status. `LINGUISTICS_REDUCED_MOTION=1` provides the same behavior before a profile exists or for managed launches.

Automated tests verify the profile preference remains backward-compatible with earlier schema-5 files and that principal text, muted text, action, navigation, danger, and focus color pairs meet their intended contrast thresholds in both themes. This is not a substitute for native scalable-text, keyboard, VoiceOver, Narrator, or visual inspection on the frozen artifact.
