using System.Diagnostics;
using Xunit;

namespace DerlictEmpires.E2E;

/// <summary>
/// xUnit fixture that starts a single Godot instance for all E2E tests.
/// Skips with a warning if GODOT_BIN env var is not set.
/// </summary>
public sealed class GodotFixture : IAsyncLifetime
{
    private Process? _process;
    private BridgeClient? _bridge;
    private readonly List<string> _stdoutLines = new();

    /// <summary>The bridge client connected to Godot.</summary>
    public BridgeClient Bridge => _bridge ?? throw new InvalidOperationException("Godot not started");

    /// <summary>Whether Godot is available and running.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Reason Godot is unavailable (for skip messages).</summary>
    public string? SkipReason { get; private set; }

    /// <summary>Path to the Godot project directory.</summary>
    public string ProjectPath { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var godotBin = Environment.GetEnvironmentVariable("GODOT_BIN");
        if (string.IsNullOrEmpty(godotBin))
        {
            SkipReason = "GODOT_BIN environment variable not set — skipping E2E tests";
            return;
        }

        if (!File.Exists(godotBin))
        {
            SkipReason = $"GODOT_BIN points to non-existent file: {godotBin}";
            return;
        }

        // Find project path — walk up from test assembly location
        ProjectPath = FindProjectPath()
            ?? throw new InvalidOperationException("Could not find project.godot");

        // Start Godot headless
        var args = new List<string>
        {
            "--headless",
            "--path", ProjectPath,
            "res://scenes/map/main.tscn"
        };

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = godotBin,
                Arguments = string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                lock (_stdoutLines) _stdoutLines.Add(e.Data);
        };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                lock (_stdoutLines) _stdoutLines.Add($"[stderr] {e.Data}");
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Connect bridge
        _bridge = new BridgeClient();
        try
        {
            await _bridge.ConnectAsync(timeoutMs: 30000);
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            SkipReason = $"Bridge connection failed: {ex.Message}. Stdout: {GetRecentStdout()}";
            _bridge.Dispose();
            _bridge = null;
            KillProcess();
        }
    }

    public async Task DisposeAsync()
    {
        _bridge?.Dispose();
        _bridge = null;
        KillProcess();
        await Task.CompletedTask;
    }

    public string GetRecentStdout(int lines = 20)
    {
        lock (_stdoutLines)
        {
            return string.Join("\n", _stdoutLines.TakeLast(lines));
        }
    }

    private void KillProcess()
    {
        if (_process == null || _process.HasExited) return;
        try
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5000);
        }
        catch { /* best effort */ }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private static string? FindProjectPath()
    {
        // Walk up from the assembly directory to find project.godot
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "project.godot");
            if (File.Exists(candidate))
                return dir;

            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        // Fallback: try GODOT_PROJECT_PATH env
        var envPath = Environment.GetEnvironmentVariable("GODOT_PROJECT_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(Path.Combine(envPath, "project.godot")))
            return envPath;

        return null;
    }
}

/// <summary>
/// Collection definition that ensures one Godot instance per test run.
/// </summary>
[CollectionDefinition("Godot")]
public class GodotCollection : ICollectionFixture<GodotFixture> { }
