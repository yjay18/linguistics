# Generated authoring sources

These five full-resolution files are authoring inputs, not app resources and not publish output. The processed Preview copies live in `content/languages/de/assets/generated/` and are the only generated images shipped with the German pack.

Generation mode: built-in OpenAI image generation, new image mode, 2026-08-30.

- `market-backdrop.png`: quiet European outdoor market backdrop, warm analogue paper texture, no people, no text, no lettering.
- `learner-cutout.png`: full-body adult learner paper puppet, side-facing walking pose, photographic collage texture, clean alpha background, no text.
- `market-stall-cutout.png`: friendly market vendor and produce stall as one paper-theatre cutout, clean alpha background, no text.
- `market-foreground-cutout.png`: low botanical and crate foreground silhouette for a market stage, torn-paper edge, clean alpha background, no text.
- `success-burst-cutout.png`: restrained mint, amber, and coral paper-confetti reaction burst, clean alpha background, no text.

`content/languages/de/assets.json` records each processed file's generated provenance, generator, prompt summary, original source hash, output hash, size, transformation, and review gate. Regenerate through `tools/AssetPipeline`; never copy these originals back into the app bundle.
