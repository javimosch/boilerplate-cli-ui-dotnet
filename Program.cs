// boilerplate-cli-ui-dotnet — a .NET CLI with an embedded web UI.
//
// The command surface follows the agent-first CLI specs
// (https://cli-specs.intrane.fr):
//   cli-output-spec  data on stdout, context on stderr, exit codes 80-119,
//                    typed errors, help-json
//   cli-guide-spec   `guide`, embedded in the binary
//   cli-daemon-spec  `serve --host --port`, /_health, /_shutdown,
//                    `daemon start|stop|status`

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using BoilerplateCliUiDotnet;

// Semantic exit codes (cli-output-spec §2).
const int ExitMissingArg = 80;
const int ExitUnknownCommand = 85;
const int ExitPrecondition = 90;
const int ExitExternal = 100;

const string PidFile = "/tmp/boilerplate-cli-ui-dotnet.pid";
const string LogFile = "/tmp/boilerplate-cli-ui-dotnet.log";
const int DefaultPort = 8080;
const string DefaultHost = "127.0.0.1";

// ─── Output helpers ─────────────────────────────────────────────

void Emit(object data) => Console.WriteLine(JsonSerializer.Serialize(data));

// Emits a typed error on stdout and exits with the matching code. The exit
// status and .error.code are the same number by construction (§2, §3).
void Die(int code, string type, string message, string suggestion)
{
    Emit(new
    {
        ok = false,
        error = new
        {
            code,
            type,
            message,
            recoverable = code >= 100 && code <= 109,
            suggestions = new[] { suggestion },
        },
    });
    Environment.Exit(code);
}

// Help is context, not the answer to a query, so it goes to stderr and stdout
// stays clean for data (cli-output-spec §1).
void PrintHelp()
{
    var e = Console.Error;
    e.WriteLine($"{Guide.Tool} - .NET CLI with an embedded web UI");
    e.WriteLine();
    e.WriteLine("Usage:");
    e.WriteLine($"  {Guide.Tool} <command> [options]");
    e.WriteLine();
    e.WriteLine("Commands:");
    e.WriteLine("  serve [--host H] [--port N]   run the HTTP server in the foreground");
    e.WriteLine("  daemon start [--port N]       start it in the background");
    e.WriteLine("  daemon stop [--port N]        stop the background server");
    e.WriteLine("  daemon status [--port N]      report background server status");
    e.WriteLine("  guide [--human]               the embedded operator guide");
    e.WriteLine("  help-json                     machine-readable command catalog");
    e.WriteLine("  version [--json]              show version information");
    e.WriteLine("  help                          show this help message");
    e.WriteLine();
    e.WriteLine("Endpoints:");
    e.WriteLine("  GET  /            Web UI");
    e.WriteLine("  GET  /api/status  Server status (JSON)");
    e.WriteLine("  GET  /_health     Liveness: {ok,service,pid}");
    e.WriteLine("  POST /_shutdown   Stop the server (token-gated off-loopback)");
    e.WriteLine();
    e.WriteLine("Exit codes: 0 ok, 80-89 input, 90-99 state, 100-109 external, 110-119 internal");
}

// ─── Flags ──────────────────────────────────────────────────────

bool HasFlag(string[] a, string name) => a.Contains(name);

// Reads --name value or --name=value.
string? FlagValue(string[] a, string name)
{
    var prefix = name + "=";
    for (var i = 0; i < a.Length; i++)
    {
        if (a[i] == name && i + 1 < a.Length) return a[i + 1];
        if (a[i].StartsWith(prefix)) return a[i][prefix.Length..];
    }
    return null;
}

// The host default MUST be loopback (cli-daemon-spec §1): serving the whole
// network is a deliberate act, never something that happens because nobody
// passed a flag.
string ResolveHost(string[] a)
{
    var flag = FlagValue(a, "--host") ?? FlagValue(a, "-host");
    if (!string.IsNullOrEmpty(flag)) return flag;
    var env = Environment.GetEnvironmentVariable("HOST");
    return string.IsNullOrEmpty(env) ? DefaultHost : env;
}

int ResolvePort(string[] a)
{
    var raw = FlagValue(a, "--port") ?? FlagValue(a, "-port") ?? FlagValue(a, "-p")
              ?? Environment.GetEnvironmentVariable("PORT");
    if (string.IsNullOrEmpty(raw)) return DefaultPort;
    if (!int.TryParse(raw, out var port) || port <= 0)
    {
        Die(ExitMissingArg, "bad_flag_value",
            $"--port must be a number, got \"{raw}\"",
            $"{Guide.Tool} serve --port 8080");
    }
    return port;
}

bool IsLoopback(string host) =>
    host is "127.0.0.1" or "localhost" or "::1";

// ─── Daemon lifecycle (cli-daemon-spec §4) ──────────────────────
//
// /_health is the source of truth for liveness, not the pid file, which goes
// stale when a process dies without cleaning up. Every subcommand is idempotent.

async Task<bool> ProbeHealth(int port)
{
    using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
    try
    {
        var resp = await client.GetAsync($"http://127.0.0.1:{port}/_health");
        return (int)resp.StatusCode == 200;
    }
    catch
    {
        return false;
    }
}

// Polls every 100ms for up to 5s, rather than sleeping a fixed amount (§4).
async Task<bool> WaitForHealth(int port, bool want)
{
    for (var i = 0; i < 50; i++)
    {
        if (await ProbeHealth(port) == want) return true;
        await Task.Delay(100);
    }
    return false;
}

int ReadPid()
{
    try
    {
        return int.TryParse(File.ReadAllText(PidFile).Trim(), out var pid) ? pid : 0;
    }
    catch
    {
        return 0;
    }
}

void RemovePid()
{
    try
    {
        File.Delete(PidFile);
    }
    catch
    {
        // already gone
    }
}

// Idempotent: an already-healthy port means report it and succeed, rather than
// racing a second process onto it (§4).
async Task DaemonStart(string host, int port)
{
    if (await ProbeHealth(port))
    {
        Emit(new { ok = true, running = true, already_running = true, port });
        return;
    }

    // The shell backgrounds the server and exits immediately, so the daemon is
    // orphaned to init rather than dying with the CLI that started it; setsid
    // gives it its own session.
    var exe = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
    var cmd = $"nohup setsid \"{exe}\" serve --host {host} --port {port} >> {LogFile} 2>&1 & echo $! > {PidFile}";

    using (var p = Process.Start(new ProcessStartInfo("/bin/sh", $"-c \"{cmd.Replace("\"", "\\\"")}\"")
           {
               UseShellExecute = false,
           }))
    {
        p?.WaitForExit();
    }

    if (!await WaitForHealth(port, true))
    {
        var pid = ReadPid();
        if (pid > 0)
        {
            Process.Start(new ProcessStartInfo("/bin/sh", $"-c \"kill {pid} 2>/dev/null\""))?.WaitForExit();
        }
        RemovePid();
        Die(ExitExternal, "daemon_unhealthy",
            $"started but /_health never answered on port {port} (see {LogFile})",
            $"{Guide.Tool} serve --port {port}");
    }

    Emit(new
    {
        ok = true,
        running = true,
        already_running = false,
        pid = ReadPid(),
        port,
        log = LogFile,
    });
}

// A no-op success when nothing is running: an agent stopping an already-stopped
// daemon has got what it asked for (§4).
async Task DaemonStop(int port)
{
    if (!await ProbeHealth(port))
    {
        RemovePid();
        Emit(new { ok = true, running = false, stopped = false, port });
        return;
    }

    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
    var request = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{port}/_shutdown");
    var token = Environment.GetEnvironmentVariable("SHUTDOWN_TOKEN");
    if (!string.IsNullOrEmpty(token))
    {
        request.Headers.Add("X-Shutdown-Token", token);
    }

    int status;
    try
    {
        var resp = await client.SendAsync(request);
        status = (int)resp.StatusCode;
    }
    catch (Exception ex)
    {
        Die(ExitExternal, "shutdown_failed", $"POST /_shutdown failed: {ex.Message}",
            $"{Guide.Tool} daemon status --port {port}");
        return;
    }

    if (status != 200)
    {
        Die(ExitExternal, "shutdown_refused", $"POST /_shutdown returned {status}",
            "set SHUTDOWN_TOKEN if the daemon is bound off-loopback");
    }

    await WaitForHealth(port, false);
    RemovePid();
    Emit(new { ok = true, running = false, stopped = true, port });
}

// Status only ever reads — it never carries the shutdown token (§4).
async Task DaemonStatus(int port)
{
    if (!await ProbeHealth(port))
    {
        Emit(new { ok = true, running = false, port });
        return;
    }
    Emit(new { ok = true, running = true, pid = ReadPid(), port, log = LogFile });
}

// ─── Server ─────────────────────────────────────────────────────

void RunServer(string host, int port)
{
    var builder = WebApplication.CreateBuilder(args);

    // ASP.NET Core logs to stdout by default, which would put context where an
    // agent expects data (cli-output-spec §1).
    builder.Logging.ClearProviders();

    // UseUrls with the requested host, not 0.0.0.0: a server told to serve
    // localhost must not be reachable from the whole network (§1).
    builder.WebHost.UseUrls($"http://{host}:{port}");

    var app = builder.Build();
    var startTime = DateTime.UtcNow;

    var assembly = Assembly.GetExecutingAssembly();
    const string resourcePrefix = "BoilerplateCliUiDotnet.wwwroot";

    string GetContentType(string path) =>
        path.EndsWith(".html") ? "text/html" :
        path.EndsWith(".js") ? "application/javascript" :
        path.EndsWith(".css") ? "text/css" :
        path.EndsWith(".json") ? "application/json" :
        path.EndsWith(".png") ? "image/png" :
        path.EndsWith(".jpg") || path.EndsWith(".jpeg") ? "image/jpeg" :
        path.EndsWith(".svg") ? "image/svg+xml" :
        "application/octet-stream";

    // ─── Daemon lifecycle (§2, §3) ──────────────────────────────
    // Open and cheap: liveness only, no dependency checks.
    app.MapGet("/_health", () => Results.Json(new
    {
        ok = true,
        service = Guide.Tool,
        pid = Environment.ProcessId,
        port,
    }));

    app.MapPost("/_shutdown", (HttpContext context) =>
    {
        var token = Environment.GetEnvironmentVariable("SHUTDOWN_TOKEN") ?? "";
        var authorized = IsLoopback(host) ||
                         (token.Length > 0 &&
                          context.Request.Headers["X-Shutdown-Token"] == token);

        if (!authorized)
        {
            // 403, and the process MUST NOT stop.
            return Results.Json(new
            {
                ok = false,
                error = new
                {
                    code = 90,
                    type = "forbidden",
                    message = "X-Shutdown-Token required when bound off-loopback",
                    recoverable = false,
                },
            }, statusCode: 403);
        }

        // Answer before exiting, so the caller learns the request was accepted.
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            RemovePid();
            Environment.Exit(0);
        });

        return Results.Json(new { ok = true, stopping = true });
    });

    // ─── The guide over HTTP (cli-guide-spec §3) ────────────────
    app.MapGet("/guide", () => Results.Content(Guide.GuideJson, "application/json"));
    app.MapGet("/llms.txt", () => Results.Content(Guide.LlmsTxt, "text/plain; charset=utf-8"));

    // ─── App API ────────────────────────────────────────────────
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
            version = Guide.Version,
            start_time = startTime.ToString("o"),
        });
    });

    app.MapGet("/api/health", () => Results.Json(new
    {
        ok = true,
        service = Guide.Tool,
        pid = Environment.ProcessId,
        port,
    }));

    // ─── Embedded UI ────────────────────────────────────────────
    app.MapGet("/", async (HttpContext context) =>
    {
        var stream = assembly.GetManifestResourceStream($"{resourcePrefix}.index.html");
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
        var resourceName = $"{resourcePrefix}.{path.Replace("/", ".")}";
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

    // Startup lines are context — stderr, never stdout (§1).
    Console.Error.WriteLine($"{Guide.Tool} serving on http://{host}:{port}/");
    Console.Error.WriteLine($"  API: http://{host}:{port}/api/status");

    try
    {
        app.Run();
    }
    catch (IOException ex)
    {
        Die(ExitPrecondition, "port_unavailable",
            $"cannot bind {host}:{port}: {ex.Message}",
            $"{Guide.Tool} serve --port {port + 1}");
    }
}

// ─── Main ───────────────────────────────────────────────────────

if (args.Length == 0)
{
    PrintHelp();
    return ExitMissingArg;
}

var command = args[0];
var rest = args.Skip(1).ToArray();

switch (command)
{
    case "help":
    case "--help":
    case "-h":
        PrintHelp();
        break;

    case "version":
        if (HasFlag(rest, "--json"))
        {
            Emit(new { version = Guide.Version, name = Guide.Tool });
        }
        else
        {
            Console.WriteLine($"{Guide.Tool} v{Guide.Version}");
        }
        break;

    case "guide":
        Console.WriteLine(HasFlag(rest, "--human") ? Guide.GuideMarkdown : Guide.GuideJson);
        break;

    case "help-json":
        Console.WriteLine(Guide.HelpJson);
        break;

    case "serve":
        RunServer(ResolveHost(rest), ResolvePort(rest));
        break;

    case "daemon":
    {
        if (rest.Length == 0)
        {
            Die(ExitMissingArg, "missing_argument",
                "daemon needs a subcommand: start, stop or status",
                $"{Guide.Tool} daemon status");
        }
        var sub = rest[0];
        var tail = rest.Skip(1).ToArray();
        var daemonPort = ResolvePort(tail);
        switch (sub)
        {
            case "start":
                await DaemonStart(ResolveHost(tail), daemonPort);
                break;
            case "stop":
                await DaemonStop(daemonPort);
                break;
            case "status":
                await DaemonStatus(daemonPort);
                break;
            default:
                Die(ExitUnknownCommand, "unknown_command",
                    $"unknown daemon subcommand \"{sub}\"",
                    $"{Guide.Tool} daemon status");
                break;
        }
        break;
    }

    // Back-compat aliases for the pre-spec command names.
    case "start":
        if (HasFlag(rest, "-daemon") || HasFlag(rest, "--daemon"))
        {
            await DaemonStart(ResolveHost(rest), ResolvePort(rest));
        }
        else
        {
            RunServer(ResolveHost(rest), ResolvePort(rest));
        }
        break;

    case "stop":
        await DaemonStop(ResolvePort(rest));
        break;

    case "status":
        await DaemonStatus(ResolvePort(rest));
        break;

    default:
        Die(ExitUnknownCommand, "unknown_command",
            $"unknown command \"{command}\"",
            $"{Guide.Tool} help-json");
        break;
}

return 0;
