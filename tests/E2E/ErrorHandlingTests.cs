using Xunit;
using Xunit.Abstractions;

namespace DerlictEmpires.E2E;

/// <summary>
/// Tests that the bridge handles bad input gracefully without crashing Godot.
/// </summary>
[Collection("Godot")]
[Trait("Category", "Headless")]
public class ErrorHandlingTests : E2ETestBase
{
    public ErrorHandlingTests(GodotFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

    [Fact]
    public async Task LoadState_NonexistentFile_ReturnsError()
    {
        // Loading a file that doesn't exist should return an error, not crash
        if (ShouldSkip()) return;

        var resp = await Bridge.LoadStateFromFileAsync(@"C:\nonexistent\fake_save.json");
        Assert.False(resp.GetProperty("ok").GetBoolean());

        var error = resp.GetProperty("error").GetString();
        Assert.NotNull(error);
        Output.WriteLine($"Expected error: {error}");

        // Bridge should still be responsive
        var ping = await Bridge.PingAsync();
        Assert.True(ping.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task LoadState_InvalidJson_ReturnsError()
    {
        // Sending garbage JSON inline should return an error, not crash
        if (ShouldSkip()) return;

        var resp = await Bridge.SendCommandAsync(new { cmd = "load_state", json = "not valid json at all" });
        Assert.False(resp.GetProperty("ok").GetBoolean());

        // Bridge should still be responsive
        var ping = await Bridge.PingAsync();
        Assert.True(ping.GetProperty("ok").GetBoolean());
    }
}
