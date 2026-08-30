# PaperStage developer fixture assets

These images are generated developer-only visual fixtures for the Phase 1 PaperStage sandbox. They are not reviewed lesson content and are not part of a language content pack.

Phase 2 also reuses these files only in the synthetic template gallery so the three
renderers can be visually inspected before the asset pipeline exists. The real German
preview lesson deliberately omits asset references and exercises the complete authored
text-only path. This reuse does not promote the fixtures into curriculum content.

Generation mode: built-in OpenAI image generation, new image mode, 2026-08-30.

Prompt set:

- `market-backdrop.png`: quiet European outdoor market backdrop, warm analogue paper texture, no people, no text, no lettering.
- `learner-cutout.png`: full-body adult learner paper puppet, side-facing walking pose, photographic collage texture, clean alpha background, no text.
- `market-stall-cutout.png`: friendly market vendor and produce stall as one paper-theatre cutout, clean alpha background, no text.
- `market-foreground-cutout.png`: low botanical and crate foreground silhouette for a market stage, torn-paper edge, clean alpha background, no text.
- `success-burst-cutout.png`: restrained mint, amber, and coral paper-confetti reaction burst, clean alpha background, no text.

If any fixture is promoted into a lesson pack, Phase 3 must create the generated-asset manifest entry, size and hash validation, prompt summary, and human review record before runtime use.
