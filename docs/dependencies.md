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

The application itself has no analytics, account, backend, database, model, or speech dependency through Milestone 2. The deterministic curriculum core uses only the .NET base class library.
