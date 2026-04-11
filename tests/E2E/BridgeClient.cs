using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace DerlictEmpires.E2E;

/// <summary>
/// Lightweight TCP client that speaks to Godot's McpBridge on port 9876.
/// Sends newline-delimited JSON commands, receives newline-delimited JSON responses.
/// </summary>
public sealed class BridgeClient : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamWriter? _writer;
    private StreamReader? _reader;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public bool IsConnected => _client?.Connected == true;

    /// <summary>
    /// Connect to the McpBridge TCP server.
    /// </summary>
    public async Task ConnectAsync(int port = 9876, int timeoutMs = 30000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", port);
                _client.NoDelay = true;
                _stream = _client.GetStream();
                _writer = new StreamWriter(_stream, new UTF8Encoding(false)) { AutoFlush = true };
                _reader = new StreamReader(_stream, Encoding.UTF8);

                // Verify with ping
                var resp = await SendCommandAsync(new { cmd = "ping" });
                if (resp.TryGetProperty("ok", out var ok) && ok.GetBoolean() &&
                    resp.TryGetProperty("pong", out var pong) && pong.GetBoolean())
                {
                    return;
                }

                // Ping failed, retry
                Dispose();
            }
            catch
            {
                _client?.Dispose();
                _client = null;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Could not connect to McpBridge within {timeoutMs}ms");
    }

    /// <summary>
    /// Send a command and wait for the response.
    /// </summary>
    public async Task<JsonElement> SendCommandAsync(object command, int timeoutMs = 30000)
    {
        if (_writer == null || _reader == null)
            throw new InvalidOperationException("Not connected");

        var json = JsonSerializer.Serialize(command, JsonOpts);
        await _writer.WriteLineAsync(json);

        using var cts = new CancellationTokenSource(timeoutMs);
        var line = await _reader.ReadLineAsync(cts.Token)
            ?? throw new IOException("Bridge connection closed");

        return JsonDocument.Parse(line).RootElement;
    }

    // ── Convenience methods ──────────────────────────────────────

    public Task<JsonElement> PingAsync() =>
        SendCommandAsync(new { cmd = "ping" });

    public Task<JsonElement> GetSceneTreeAsync() =>
        SendCommandAsync(new { cmd = "tree" });

    public Task<JsonElement> GetLogsAsync() =>
        SendCommandAsync(new { cmd = "logs" });

    public Task<JsonElement> GetStdoutAsync(int lines = 50) =>
        SendCommandAsync(new { cmd = "stdout", lines });

    public Task<JsonElement> LoadStateFromFileAsync(string path) =>
        SendCommandAsync(new { cmd = "load_state", path });

    public Task<JsonElement> LoadStateFromJsonAsync(string jsonState) =>
        SendCommandAsync(new { cmd = "load_state", json = JsonSerializer.Deserialize<JsonElement>(jsonState) });

    public Task<JsonElement> SaveStateAsync(string? path = null) =>
        path != null
            ? SendCommandAsync(new { cmd = "save_state", path })
            : SendCommandAsync(new { cmd = "save_state" });

    public Task<JsonElement> ScreenshotAsync() =>
        SendCommandAsync(new { cmd = "screenshot" });

    public Task<JsonElement> FindNodesAsync(string type) =>
        SendCommandAsync(new { cmd = "nodes", type });

    public Task<JsonElement> TickAsync(int fast = 0, int slow = 0) =>
        SendCommandAsync(new { cmd = "tick", fast, slow });

    public void Dispose()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _stream?.Dispose();
        _client?.Dispose();
        _client = null;
    }
}
