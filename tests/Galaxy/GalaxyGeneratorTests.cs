using Xunit;
using Xunit.Abstractions;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Tests.Galaxy;

public class GalaxyGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public GalaxyGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static GalaxyGenerationConfig DefaultConfig(int seed = 42) => new()
    {
        Seed = seed,
        TotalSystems = 100,
        ArmCount = 4,
        GalaxyRadius = 200f,
        MaxLaneLength = 60f,
        MinNeighbors = 2,
        MaxNeighbors = 4,
        HiddenLaneRatio = 0.10f
    };

    [Fact]
    public void SameSeed_ProducesIdenticalGalaxy()
    {
        var galaxy1 = GalaxyGenerator.Generate(DefaultConfig(42));
        var galaxy2 = GalaxyGenerator.Generate(DefaultConfig(42));

        Assert.Equal(galaxy1.Systems.Count, galaxy2.Systems.Count);
        Assert.Equal(galaxy1.Lanes.Count, galaxy2.Lanes.Count);

        for (int i = 0; i < galaxy1.Systems.Count; i++)
        {
            Assert.Equal(galaxy1.Systems[i].Name, galaxy2.Systems[i].Name);
            Assert.Equal(galaxy1.Systems[i].PositionX, galaxy2.Systems[i].PositionX);
            Assert.Equal(galaxy1.Systems[i].PositionZ, galaxy2.Systems[i].PositionZ);
            Assert.Equal(galaxy1.Systems[i].ArmIndex, galaxy2.Systems[i].ArmIndex);
            Assert.Equal(galaxy1.Systems[i].IsCore, galaxy2.Systems[i].IsCore);
            Assert.Equal(galaxy1.Systems[i].POIs.Count, galaxy2.Systems[i].POIs.Count);
        }
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentGalaxy()
    {
        var galaxy1 = GalaxyGenerator.Generate(DefaultConfig(42));
        var galaxy2 = GalaxyGenerator.Generate(DefaultConfig(999));

        // Names should differ (overwhelmingly likely with different seeds)
        bool anyDifferent = false;
        int checkCount = Math.Min(galaxy1.Systems.Count, galaxy2.Systems.Count);
        for (int i = 0; i < checkCount; i++)
        {
            if (galaxy1.Systems[i].Name != galaxy2.Systems[i].Name)
            {
                anyDifferent = true;
                break;
            }
        }
        Assert.True(anyDifferent);
    }

    [Fact]
    public void SystemCount_MatchesConfig()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);
        Assert.Equal(config.TotalSystems, galaxy.Systems.Count);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    public void SystemCount_ScalesWithConfig(int total)
    {
        var config = DefaultConfig();
        config.TotalSystems = total;
        var galaxy = GalaxyGenerator.Generate(config);
        Assert.Equal(total, galaxy.Systems.Count);
    }

    [Fact]
    public void CoreSystems_AreAbout20Percent()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        int coreCount = galaxy.Systems.Count(s => s.IsCore);
        // Should be ~20 for 100 total systems (20%)
        Assert.InRange(coreCount, 10, 30);
    }

    [Fact]
    public void EachArm_HasSystems()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);

        for (int arm = 0; arm < config.ArmCount; arm++)
        {
            int armCount = galaxy.Systems.Count(s => s.ArmIndex == arm);
            Assert.True(armCount > 0, $"Arm {arm} has no systems");
        }
    }

    [Fact]
    public void AllSystems_HaveUniqueIds()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        var ids = galaxy.Systems.Select(s => s.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void AllSystems_HaveNames()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        foreach (var sys in galaxy.Systems)
        {
            Assert.False(string.IsNullOrWhiteSpace(sys.Name), $"System {sys.Id} has no name");
        }
    }

    [Fact]
    public void NoStars_AreTooClose()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        float minDist = float.MaxValue;
        string? pair = null;

        for (int i = 0; i < galaxy.Systems.Count; i++)
        {
            for (int j = i + 1; j < galaxy.Systems.Count; j++)
            {
                var a = galaxy.Systems[i];
                var b = galaxy.Systems[j];
                float dx = a.PositionX - b.PositionX;
                float dz = a.PositionZ - b.PositionZ;
                float d = MathF.Sqrt(dx * dx + dz * dz);
                if (d < minDist)
                {
                    minDist = d;
                    pair = $"{a.Name} (id={a.Id}) <-> {b.Name} (id={b.Id})";
                }
            }
        }

        _output.WriteLine($"Min distance: {minDist:F2} between {pair}");
        Assert.True(minDist >= 4.5f, $"Stars too close: {minDist:F2} between {pair}");
    }

    [Fact]
    public void Diagnostic_SpiralArmShape()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        // Per-arm stats
        for (int arm = 0; arm < 4; arm++)
        {
            var armSys = galaxy.Systems.Where(s => s.ArmIndex == arm).ToList();
            _output.WriteLine($"Arm {arm}: {armSys.Count} stars");
        }
        var coreSys = galaxy.Systems.Where(s => s.IsCore).ToList();
        _output.WriteLine($"Core: {coreSys.Count} stars");

        // ASCII map
        int w = 72, h = 36;
        var grid = new char[h, w];
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
                grid[r, c] = ' ';

        var armChars = new Dictionary<int, char>
        {
            { -1, 'O' }, { 0, '1' }, { 1, '2' }, { 2, '3' }, { 3, '4' }
        };

        foreach (var s in galaxy.Systems)
        {
            int c = (int)((s.PositionX + 220) / 440 * w);
            int r = (int)((s.PositionZ + 220) / 440 * h);
            c = Math.Clamp(c, 0, w - 1);
            r = Math.Clamp(r, 0, h - 1);
            grid[r, c] = armChars.GetValueOrDefault(s.ArmIndex, '*');
        }

        _output.WriteLine("");
        _output.WriteLine("Galaxy map (O=core, 1-4=arm index):");
        for (int r = 0; r < h; r++)
        {
            var line = new char[w];
            for (int c = 0; c < w; c++)
                line[c] = grid[r, c];
            _output.WriteLine(new string(line));
        }
    }
}
