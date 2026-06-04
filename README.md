# boilerplate-cli-ui-dotnet

.NET 8 CLI with embedded web UI. Single binary, no runtime dependencies.

Part of [SuperCLI](https://github.com/javimosch/supercli) - build CLI/UI plugins fast for 2026.

**Other versions**: [Go+Vue](https://github.com/javimosch/boilerplate-cli-ui-go-v2-vue) | [Go+React](https://github.com/javimosch/boilerplate-cli-ui-go-v2-react) | [Rust](https://github.com/javimosch/boilerplate-cli-ui-rust) | [C++](https://github.com/javimosch/boilerplate-cli-ui-cpp) | [Nim](https://github.com/javimosch/boilerplate-cli-ui-nim) | [Node](https://github.com/javimosch/boilerplate-cli-ui-node) | [Python](https://github.com/javimosch/boilerplate-cli-ui-python)

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
```

## Key Feature: EmbeddedResource

Frontend files are **embedded into the binary** at compile time:

```xml
<EmbeddedResource Include="wwwroot\**\*" />
```

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new EmbeddedFileProvider(assembly)
});
```

**Benefits:**
- Single binary output (no runtime file dependencies)
- Wildcard embeds entire directory tree (like Go's `go:embed`)
- Add new files → just create them (no code changes)
- Same modularity as Go version

## Build

```bash
chmod +x build.sh
./build.sh
```

Or manually:

```bash
dotnet publish -c Release --self-contained -r linux-x64 -o ./dist
```

## Cross-Compile

```bash
# Linux x64
dotnet publish -c Release --self-contained -r linux-x64 -o ./dist/linux

# macOS ARM64 (Apple Silicon)
dotnet publish -c Release --self-contained -r osx-arm64 -o ./dist/mac

# Windows x64
dotnet publish -c Release --self-contained -r win-x64 -o ./dist/win

# Linux ARM64 (Raspberry Pi)
dotnet publish -c Release --self-contained -r linux-arm64 -o ./dist/arm
```

## Usage

```bash
# Start server (foreground)
./dist/linux/boilerplate-cli-ui-dotnet start

# Start on custom port
./dist/linux/boilerplate-cli-ui-dotnet start -p 3000

# Show version
./dist/linux/boilerplate-cli-ui-dotnet version

# Show help
./dist/linux/boilerplate-cli-ui-dotnet help
```

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
| Binary size | ~5MB | ~1.1MB | ~89MB |
| Embed pattern | `//go:embed` | `include_str!` | `EmbeddedResource` |
| Modularity | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| Compile time | Fast | Medium | Medium |
| Cross-compile | Easy | Easy | Easy |
