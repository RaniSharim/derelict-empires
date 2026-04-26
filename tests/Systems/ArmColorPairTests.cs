using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Systems;
using Xunit;

namespace DerlictEmpires.Tests.Systems;

public class ArmColorPairTests
{
    [Fact]
    public void TwoArms_TwoPairs()
    {
        var pairs = GalaxyGenerator.BuildArmColorPairs(2);
        Assert.Equal(2, pairs.Count);
        Assert.Equal(PrecursorColor.Red, pairs[0].Primary);
        Assert.Equal(PrecursorColor.Blue, pairs[0].Secondary);
        Assert.Equal(PrecursorColor.Blue, pairs[1].Primary);
        Assert.Equal(PrecursorColor.Green, pairs[1].Secondary);
    }

    [Fact]
    public void FiveArms_CycleClosesOnRed()
    {
        var pairs = GalaxyGenerator.BuildArmColorPairs(5);
        Assert.Equal(5, pairs.Count);
        Assert.Equal(PrecursorColor.Purple, pairs[4].Primary);
        Assert.Equal(PrecursorColor.Red, pairs[4].Secondary);
    }

    [Fact]
    public void EveryColor_AppearsAsPrimaryAndSecondary_AcrossFiveArms()
    {
        var pairs = GalaxyGenerator.BuildArmColorPairs(5);
        var primaries = new System.Collections.Generic.HashSet<PrecursorColor>();
        var secondaries = new System.Collections.Generic.HashSet<PrecursorColor>();
        foreach (var p in pairs)
        {
            primaries.Add(p.Primary);
            secondaries.Add(p.Secondary);
        }
        Assert.Equal(5, primaries.Count);
        Assert.Equal(5, secondaries.Count);
    }

    [Fact]
    public void SixArms_WrapsAroundAndRepeatsRed()
    {
        var pairs = GalaxyGenerator.BuildArmColorPairs(6);
        Assert.Equal(PrecursorColor.Red, pairs[0].Primary);
        Assert.Equal(PrecursorColor.Red, pairs[5].Primary); // 6 % 5 = 1? — actually 5 % 5 = 0
        Assert.Equal(pairs[0].Primary, pairs[5].Primary);
        Assert.Equal(pairs[0].Secondary, pairs[5].Secondary);
    }

    [Fact]
    public void GalaxyData_ResolvesPairForArmIndex_AndNullForCore()
    {
        var galaxy = new DerlictEmpires.Core.Models.GalaxyData
        {
            ArmColorPairs = GalaxyGenerator.BuildArmColorPairs(3),
        };
        Assert.NotNull(galaxy.GetArmPair(0));
        Assert.Equal(PrecursorColor.Red, galaxy.GetArmPair(0)!.Value.Primary);
        Assert.Null(galaxy.GetArmPair(-1));
        Assert.Null(galaxy.GetArmPair(99));
    }
}
