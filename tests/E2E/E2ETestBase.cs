using Xunit;
using Xunit.Abstractions;

namespace DerlictEmpires.E2E;

/// <summary>
/// Base class for E2E tests. Provides bridge access and skip-if-unavailable logic.
/// </summary>
[Collection("Godot")]
public abstract class E2ETestBase
{
    protected readonly GodotFixture Fixture;
    protected readonly ITestOutputHelper Output;
    protected BridgeClient Bridge => Fixture.Bridge;

    protected E2ETestBase(GodotFixture fixture, ITestOutputHelper output)
    {
        Fixture = fixture;
        Output = output;
    }

    /// <summary>
    /// Returns true if Godot is unavailable and the test should be skipped.
    /// Logs a warning to test output.
    /// </summary>
    protected bool ShouldSkip()
    {
        if (!Fixture.IsAvailable)
        {
            Output.WriteLine($"SKIP: {Fixture.SkipReason}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resolve a fixture file path relative to the test output directory.
    /// </summary>
    protected string FixturePath(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath);
    }
}
