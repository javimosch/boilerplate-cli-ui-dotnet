using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ─── CLI Arguments ──────────────────────────────────────────────
if (args.Contains("version"))
{
    Console.WriteLine("boilerplate-cli-ui-dotnet v1.0.0");
    return;
}

if (args.Contains("help") || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("boilerplate-cli-ui-dotnet - .NET CLI with embedded web UI");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  boilerplate-cli-ui-dotnet <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  start       Start HTTP server with web UI");
    Console.WriteLine("  version     Show version information");
    Console.WriteLine("  help        Show this help message");
    Console.WriteLine();
    Console.WriteLine("Start Options:");
    Console.WriteLine("  -p, --port <PORT>  Port for HTTP server (default 8080)");
    Console.WriteLine();
    Console.WriteLine("API Endpoints:");
    Console.WriteLine("  GET /            Web UI");
    Console.WriteLine("  GET /api/status  Server status (JSON)");
    Console.WriteLine("  GET /api/health  Health check (JSON)");
    return;
}

// Parse port from args
int port = 8080;
for (int i = 0; i < args.Length - 1; i++)
{
    if ((args[i] == "-p" || args[i] == "--port") && int.TryParse(args[i + 1], out int p))
    {
        port = p;
        break;
    }
}

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// ─── Start Time (for uptime) ────────────────────────────────────
var startTime = DateTime.UtcNow;

// ─── Embedded Resources Helper ──────────────────────────────────
var assembly = Assembly.GetExecutingAssembly();
var resourcePrefix = "BoilerplateCliUiDotnet.wwwroot";

string GetContentType(string path)
{
    return path.EndsWith(".html") ? "text/html" :
           path.EndsWith(".js") ? "application/javascript" :
           path.EndsWith(".css") ? "text/css" :
           path.EndsWith(".json") ? "application/json" :
           path.EndsWith(".png") ? "image/png" :
           path.EndsWith(".jpg") || path.EndsWith(".jpeg") ? "image/jpeg" :
           path.EndsWith(".svg") ? "image/svg+xml" :
           "application/octet-stream";
}

app.MapGet("/", async (HttpContext context) =>
{
    var resourceName = $"{resourcePrefix}.index.html";
    var stream = assembly.GetManifestResourceStream(resourceName);
    if (stream != null)
    {
        context.Response.ContentType = "text/html";
        await stream.CopyToAsync(context.Response.Body);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

app.MapGet("/{*path}", async (HttpContext context, string path) =>
{
    // Convert path to resource name (replace / with .)
    var resourcePath = path.Replace("/", ".");
    var resourceName = $"{resourcePrefix}.{resourcePath}";
    
    var stream = assembly.GetManifestResourceStream(resourceName);
    if (stream != null)
    {
        context.Response.ContentType = GetContentType(path);
        await stream.CopyToAsync(context.Response.Body);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

// ─── API Endpoints ──────────────────────────────────────────────
app.MapGet("/api/status", () =>
{
    var elapsed = DateTime.UtcNow - startTime;
    var uptime = elapsed.TotalHours >= 1
        ? $"{(int)elapsed.TotalHours}h{elapsed.Minutes}m{elapsed.Seconds}s"
        : elapsed.TotalMinutes >= 1
            ? $"{(int)elapsed.TotalMinutes}m{elapsed.Seconds}s"
            : $"{elapsed.Seconds}s";

    return Results.Json(new
    {
        status = "running",
        port,
        uptime,
        version = "1.0.0",
        start_time = startTime.ToString("o")
    });
});

app.MapGet("/api/health", () => Results.Json(new
{
    status = "healthy",
    version = "1.0.0"
}));

// ─── Start ──────────────────────────────────────────────────────
Console.WriteLine($"Server starting on http://localhost:{port}");
Console.WriteLine($"UI available at http://localhost:{port}/");
Console.WriteLine($"API available at http://localhost:{port}/api/status");
Console.WriteLine("Press Ctrl+C to stop");

app.Run();
