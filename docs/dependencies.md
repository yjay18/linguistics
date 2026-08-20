# Dependency inventory

## Production

| Dependency | Version | Purpose | License |
| --- | --- | --- | --- |
| .NET | 10.0 LTS | Runtime, base class library, build toolchain | MIT |
| Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent | 12.1.0 | Shared macOS and Windows UI, platform backends, and built-in desktop controls | MIT |

Avalonia brings its rendering and text stack transitively, including SkiaSharp 3.119.4 and HarfBuzzSharp 8.3.1.3; their NuGet packages declare MIT licenses. Exact package content hashes are recorded in `packages.lock.json` files. No optional Avalonia commercial controls, web view, custom font package, or diagnostics package is included.

## Test only

| Dependency | Version | Purpose | License |
| --- | --- | --- | --- |
| MSTest | 4.0.2 | Unit-test discovery, assertions, and execution | MIT |

The application itself has no analytics, account, backend, database, bundled model, or linked third-party speech package through Milestone 6. The deterministic curriculum, content validator, Ollama HTTP adapter, process safety layer, and transcript comparison use only the .NET base class library.

Ollama is an optional, separately installed local runtime and is not linked, bundled, started, signed into, or downloaded by Linguistics. Ollama's application code is MIT-licensed; every installed model has separate capability, storage, license, and redistribution terms that must be inspected and reviewed before the app recommends it. No model configuration is currently claimed as supported.

System speech playback is an operating-system capability: Linguistics invokes `/usr/bin/say` on macOS and Windows PowerShell plus `System.Speech.Synthesis.SpeechSynthesizer` on Windows through fixed argument lists and standard input. These tools are not redistributed by the app. Local microphone transcription optionally invokes a separately installed `whisper-stream` executable from the MIT-licensed `whisper.cpp` project. Linguistics does not bundle or download the executable or a model. Model weights remain a separately acquired artifact whose source, size, license, performance, and redistribution terms must be reviewed before use or distribution.
