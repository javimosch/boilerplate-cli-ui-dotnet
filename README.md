# boilerplate-cli-ui-dotnet

.NET 8 CLI with embedded web UI. Single binary, no runtime dependencies.
Part of [SuperCLI](https://github.com/javimosch/supercli) - build CLI/UI plugins fast for 2026.
<!-- FLEET-TABLE:BEGIN -->

| Stack | Binary | Cold start | Idle RSS | Specs | SDK |
|-------|--------|-----------:|---------:|:-----:|----:|
| [machin + React 18 CDN](https://github.com/javimosch/boilerplate-cli-ui-machin) | 63 KB | 2 ms | 3.1 MB | 28/28 | ~2 MB |
| [machin isomorphic (wasm UI)](https://github.com/javimosch/boilerplate-cli-ui-machin-isomorphic) | 76 KB | 2 ms | 3.1 MB | 28/28 | ~2 MB |
| [C++ + Vue 3](https://github.com/javimosch/boilerplate-cli-ui-cpp) | 692 KB | 3 ms | 7.2 MB | 28/28 | ~2000 MB |
| [Zig + Vue 3](https://github.com/javimosch/boilerplate-cli-ui-zig) | 971 KB | 1 ms | 2.0 MB | 28/28 | ~50 MB |
| [Rust + vanilla JS](https://github.com/javimosch/boilerplate-cli-ui-rust) | 1003 KB | 1 ms | 2.5 MB | 28/28 | ~800 MB |
| [Go + Vue 3 CDN](https://github.com/javimosch/boilerplate-cli-ui-go-v2-vue) | 5.5 MB | 2 ms | 5.9 MB | 28/28 | ~150 MB |
| [Go + React 18 CDN](https://github.com/javimosch/boilerplate-cli-ui-go-v2-react) | 5.5 MB | 2 ms | 5.8 MB | 28/28 | ~150 MB |
| [Deno + vanilla JS](https://github.com/javimosch/boilerplate-cli-ui-deno) | 76.1 MB | 24 ms | 43.8 MB | 28/28 | ~100 MB |
| [Node.js + vanilla JS](https://github.com/javimosch/boilerplate-cli-ui-node) | 122.8 MB | 66 ms | 52.2 MB | 28/28 | ~500 MB |

Not measured in this run (toolchain unavailable): [Nim + Vue 3](https://github.com/javimosch/boilerplate-cli-ui-nim), [V + Vue 3](https://github.com/javimosch/boilerplate-cli-ui-v), [Crystal + Vue 3](https://github.com/javimosch/boilerplate-cli-ui-crystal), [Dart + Vue 3](https://github.com/javimosch/boilerplate-cli-ui-dart), [Python + React CDN](https://github.com/javimosch/boilerplate-cli-ui-python), [.NET 8 + Vue 3](https://github.com/javimosch/boilerplate-cli-ui-dotnet).

*Binary size, cold start (median of 11 `version` runs) and idle RSS measured on Linux-x86_64 on 2026-09-09. **Specs** is the [cli-spec-conformance](https://github.com/javimosch/cli-spec-conformance) score across cli-output-spec, cli-guide-spec and cli-daemon-spec, taken by running each binary — not claimed. Every row builds the same reference app and implements the same agent-first contract, which is what makes the sizes comparable: a 63 KB binary that scores 28/28 is doing the work a 122 MB one does. Rows whose toolchain is missing on the measuring host are left out rather than given a stale number; each is gated on the same conformance check in its own CI. Regenerate with [boilerplate-cli-ui-fleet](https://github.com/javimosch/boilerplate-cli-ui-fleet); never edit this table by hand.*

<!-- FLEET-TABLE:END -->
## Architecture
```
boilerplate-cli-ui-dotnet/
├── Program.cs                  # CLI + HTTP server
├── wwwroot/                    # Frontend (embedded at compile time)
│   ├── index.html              # Entry point (Vue 3)
│   ├── js/
│   │   ├── app.js
│   │   ├── components/
│   │   └── views/
│   └── css/
│       └── styles.css
├── boilerplate-cli-ui-dotnet.csproj
├── build.sh
└── README.md
## Key Feature: EmbeddedResource
Frontend files are **embedded into the binary** at compile time:
```xml
<EmbeddedResource Include="wwwroot\**\*" />
```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new EmbeddedFileProvider(assembly)
});
**Benefits:**
- Single binary output (no runtime file dependencies)
- Wildcard embeds entire directory tree (like Go's `go:embed`)
- Add new files → just create them (no code changes)
- Same modularity as Go version
## Build
```bash
chmod +x build.sh
./build.sh
Or manually:
dotnet publish -c Release --self-contained -r linux-x64 -o ./dist
## Cross-Compile
# Linux x64
dotnet publish -c Release --self-contained -r linux-x64 -o ./dist/linux
# macOS ARM64 (Apple Silicon)
dotnet publish -c Release --self-contained -r osx-arm64 -o ./dist/mac
# Windows x64
dotnet publish -c Release --self-contained -r win-x64 -o ./dist/win
# Linux ARM64 (Raspberry Pi)
dotnet publish -c Release --self-contained -r linux-arm64 -o ./dist/arm
## Usage
# Start server (foreground)
./dist/linux/boilerplate-cli-ui-dotnet start
# Start on custom port
./dist/linux/boilerplate-cli-ui-dotnet start -p 3000
# Show version
./dist/linux/boilerplate-cli-ui-dotnet version
# Show help
./dist/linux/boilerplate-cli-ui-dotnet help
## API Endpoints
| Endpoint | Description |
|----------|-------------|
| `GET /` | Web UI |
| `GET /api/status` | Server status (JSON) |
| `GET /api/health` | Health check (JSON) |
## Hashbang Routing
Routes use hashbang URLs:
- `http://localhost:8080/#/dashboard` - Dashboard view
- `http://localhost:8080/#/settings` - Settings view
- `http://localhost:8080/` - Defaults to dashboard
## Frontend Stack
- **Vue 3** (CDN) - Reactive UI with hashbang routing
- **Tailwind CSS** (CDN) - Utility-first styling
- **Lucide Icons** (CDN) - Icon library
## Comparison
| Aspect | Go | Rust | .NET 8 |
|--------|-----|------|--------|
| Binary size | ~5MB | ~150MB | ~1.1MB | ~800MB | ~89MB | ~600MB |
| Embed pattern | `//go:embed` | `include_str!` | `EmbeddedResource` |
| Modularity | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| Compile time | Fast | Medium | Medium |
| Cross-compile | Easy | Easy | Easy |
