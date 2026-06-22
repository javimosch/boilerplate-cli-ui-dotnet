# boilerplate-cli-ui-dotnet

.NET 8 CLI with embedded web UI. Single binary, no runtime dependencies.
Part of [SuperCLI](https://github.com/javimosch/supercli) - build CLI/UI plugins fast for 2026.
| Stack | Repo | Binary | SDK Size |
|-------|------|--------|----------|
| Go + inline HTML | [boilerplate-cli-ui-go](https://github.com/javimosch/boilerplate-cli-ui-go) | ~5MB | ~150MB |
| Go + Vue 3 CDN | [boilerplate-cli-ui-go-v2-vue](https://github.com/javimosch/boilerplate-cli-ui-go-v2-vue) | ~5MB | ~150MB |
| Go + React 18 CDN | [boilerplate-cli-ui-go-v2-react](https://github.com/javimosch/boilerplate-cli-ui-go-v2-react) | ~5MB | ~150MB |
| Deno + vanilla JS | [boilerplate-cli-ui-deno](https://github.com/javimosch/boilerplate-cli-ui-deno) | ~76MB | ~100MB |
| Node.js + vanilla JS | [boilerplate-cli-ui-node](https://github.com/javimosch/boilerplate-cli-ui-node) | ~123MB | ~500MB+ |
| Python + React CDN | [boilerplate-cli-ui-python](https://github.com/javimosch/boilerplate-cli-ui-python) | ~10MB | ~300MB |
| Rust + vanilla JS | [boilerplate-cli-ui-rust](https://github.com/javimosch/boilerplate-cli-ui-rust) | ~1.1MB | ~800MB |
| **.NET 8 + Vue 3** | **boilerplate-cli-ui-dotnet** | **~89MB** |
| C++ + Vue 3 | [boilerplate-cli-ui-cpp](https://github.com/javimosch/boilerplate-cli-ui-cpp) | ~493KB | ~2GB+ |
| Nim + Vue 3 | [boilerplate-cli-ui-nim](https://github.com/javimosch/boilerplate-cli-ui-nim) | ~364KB | ~50MB |
| Zig + Vue 3 | [boilerplate-cli-ui-zig](https://github.com/javimosch/boilerplate-cli-ui-zig) | ~190KB | ~50MB |
| Dart + Vue 3 | [boilerplate-cli-ui-dart](https://github.com/javimosch/boilerplate-cli-ui-dart) | ~6.4MB | ~400MB |
| machin + React 18 CDN | [boilerplate-cli-ui-machin](https://github.com/javimosch/boilerplate-cli-ui-machin) | ~27KB | ~2MB |
|| V + Vue 3 | [boilerplate-cli-ui-v](https://github.com/javimosch/boilerplate-cli-ui-v) | ~1.2MB | ~5MB |
|| Crystal + Vue 3 | [boilerplate-cli-ui-crystal](https://github.com/javimosch/boilerplate-cli-ui-crystal) | ~3.1MB | ~50MB |
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
