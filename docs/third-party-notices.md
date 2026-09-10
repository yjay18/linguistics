# Third-party notices and dependency audit

This notice describes the resolved Phase 8 dependency graph. CI re-runs the audit against
each published app before preserving visual evidence. It is not legal approval of the
app's own content or distribution.

## Runtime libraries in the current publish output

| Component family | Resolved version | Declared license | Package metadata holder or source | Purpose |
| --- | --- | --- | --- | --- |
| Avalonia, Desktop, Fluent, Native, Win32, FreeDesktop, X11, Skia, HarfBuzz, Remote Protocol | 12.1.0 | MIT | The AvaloniaUI Project, [source](https://github.com/AvaloniaUI/Avalonia/) | Cross-platform desktop UI and platform backends |
| Avalonia ANGLE Windows natives | 2.1.27548.20260419 | BSD-style license file | AvaloniaUI ANGLE, [source](https://github.com/AvaloniaUI/angle/) | Windows graphics translation layer |
| SkiaSharp and macOS, Windows, Linux, and WebAssembly native assets | 3.119.4 | MIT wrapper; bundled upstream notices | Microsoft Corporation, [source](https://github.com/mono/SkiaSharp) | Runtime rendering and authoring-only image transforms |
| HarfBuzzSharp and macOS, Windows, Linux, and WebAssembly native assets | 8.3.1.3 | MIT wrapper; bundled upstream notices | Microsoft Corporation, [source](https://github.com/mono/SkiaSharp) | Text shaping |
| MicroCom.Runtime | 0.11.6 | MIT | Nikita Tsukanov, [source](https://github.com/kekekeks/MicroCom) | Native interop used by Avalonia |
| Tmds.DBus.Protocol | 0.94.1 | MIT | Tom Deseyn, [source](https://github.com/tmds/Tmds.DBus) | Linux desktop transitive support present in the portable publish set |

The resolved NuGet metadata identifies the licenses above. SkiaSharp and HarfBuzzSharp
ship an identical `THIRD-PARTY-NOTICES.txt` file (SHA-256
`21504c46c4c58aa64c1055bd2dcbc5f9a136b4b8c412ed3cc6740e22c5b127f5`); one exact
package copy is included in the app's `Content` output. The ANGLE package `LICENSE`
(SHA-256 `54aff7276217df9f6b5181613999d208c9e40d2b1d51bf55217837e6871a4a63`) is also
copied verbatim. These native notices include upstream Skia, HarfBuzz, image/font, and
related component terms and must remain with any artifact that contains those binaries.

MSTest 4.0.2 and its transitive test platform/code-coverage packages are development/test-only and are absent from the application publish output. The test graph includes Microsoft Application Insights through the test platform, but the app has no runtime reference, analytics SDK, or telemetry endpoint.

## Bundled Preview image notices

The following Wikimedia Commons photographs are processed into the German Preview pack.
The adjacent `content/languages/de/assets.json` is canonical for retrieval date, original
and processed hashes, exact transformations, license URL, intended use, and required
attribution. All license and redistribution review states remain pending.

| Bundled subject | Credited author | Reported license | Commons source |
| --- | --- | --- | --- |
| A small cup of coffee | Julius Schorzman | CC BY-SA 2.0 | [File page](https://commons.wikimedia.org/wiki/File:A_small_cup_of_coffee.JPG) |
| Cup of milky tea | Brett Taylor from Wellington, New Zealand | CC BY 2.0 | [File page](https://commons.wikimedia.org/wiki/File:Cup_of_milky_tea.jpg) |
| Glass of water, detail | Mia Gaitanidis | CC BY-SA 4.0 | [File page](https://commons.wikimedia.org/wiki/File:Glass_of_water,_detail.jpg) |
| Colouring pencils | MichaelMaggs | CC BY-SA 3.0 | [File page](https://commons.wikimedia.org/wiki/File:Colouring_pencils.jpg) |
| LH school scissors | Doggerelblogger | CC BY-SA 4.0 | [File page](https://commons.wikimedia.org/wiki/File:LH_school_scissors.jpg) |
| Hermandad, friendship handshake | Rufino | CC BY-SA 2.0 | [File page](https://commons.wikimedia.org/wiki/File:Hermandad_-_friendship.jpg) |
| Children's number blocks used for counting | perpetual.fostering | CC BY 2.0 | [File page](https://commons.wikimedia.org/wiki/File:Children%27s_number_blocks_being_used_for_learning_to_count_-_51069936953.jpg) |

All seven Commons files are downscaled and re-encoded without an authored crop or
background removal. The automatic counting-block mask candidate was rejected during
close-up QA because its alpha edge was ragged; the bundled JPEG retains the clean pale
photographic field and natural contact shadows. This factual inventory is not a finding
that the files are approved for release.

Five additional paper-stage files are generated illustrations, not third-party
photographs. They carry `LicenseRef-Generated-Internal-Draft`, generator and prompt
provenance, and pending ownership/redistribution review in `assets.json`; they are not
covered by the Commons licenses above.

The current manifest marks none of the 12 files as cropped, background-removed, or an
authored derivative. Legal review must still decide whether each downscale and re-encode
has any jurisdiction-specific adaptation consequence. The validator rejects a future
CC BY-SA asset marked as a derivative unless its share-alike obligation is explicitly
retained; that machine rule is not a substitute for the pending legal review.

## External optional software not bundled

- Ollama and any selected local model remain separate installations. Their model-specific licenses, weights, and notices are not redistributed by Linguistics.
- `whisper-stream`, whisper.cpp, and speech model weights remain separate installations. No executable or model is copied into the publish artifact.
- macOS and Windows system voices and speech frameworks are operating-system components, not bundled assets.
- The .NET 10 runtime is not contained in the current framework-dependent publish output.

## Content and product licensing blockers

The audit confirms 24 resolved runtime package identities, 12 bundled images, both source
notice copies, and both native notice hashes. It does not clear distribution. All 12 image
records still have pending license review with modification and redistribution review set
to false. The bundled language and transfer packs remain machine-validated drafts with
pending license and redistribution review, and their runtime gate prevents learner-facing
use. The repository also has no product-level license selected by its owner. These issues
block public distribution even though the dependency notices are present.

## MIT License

Copyright holders are identified by the component metadata and bundled native notice file.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
