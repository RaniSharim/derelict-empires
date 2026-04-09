using Xunit;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;

namespace DerlictEmpires.Tests.Settlements;

public class ColonyTests
{
    private static Colony MakeColony(int pop = 5, PlanetSize size = PlanetSize.Medium)
    {
        var colony = new Colony
        {
            Id = 0, Name = "Test Colony", OwnerEmpireId = 0,
            PlanetSize = size, Happiness = 70f
        };
        colony.PopGroups.Add(new PopGroup { Count = pop, Allocation = WorkPool.Food });
        return colony;
    }

    [Theory]
    [InlineData(PlanetSize.Small, 6)]
    [InlineData(PlanetSize.Medium, 8)]
    [InlineData(PlanetSize.Large, 13)]
    [InlineData(PlanetSize.Prime, 20)]
    [InlineData(PlanetSize.Exceptional, 30)]
    public void PopCap_MatchesPlanetSize(PlanetSize size, int expectedCap)
    {
        var colony = MakeColony(1, size);
        Assert.Equal(expectedCap, colony.PopCap);
    }

    [Fact]
    public void PopCap_IncreasesWithHabModule()
    {
        var colony = MakeColony(1, PlanetSize.Medium);
        int baseCap = colony.PopCap;
        colony.Buildings.Add("hab_module");
        Assert.Equal(baseCap + 3, colony.PopCap);
    }

    [Fact]
    public void PopAllocation_CalculatesCorrectOutput()
    {
        var colony = MakeColony(0);
        colony.PopGroups.Clear();
        colony.PopGroups.Add(new PopGroup { Count = 3, Allocation = WorkPool.Production });
        colony.PopGroups.Add(new PopGroup { Count = 2, Allocation = WorkPool.Research });

        Assert.Equal(9, colony.BaseProductionOutput); // 3 * 3
        Assert.Equal(4, colony.BaseResearchOutput);   // 2 * 2
        Assert.Equal(0, colony.BaseFoodOutput);
    }

    [Fact]
    public void BuildingBonus_AppliesCorrectly()
    {
        var colony = MakeColony(0);
        colony.PopGroups.Add(new PopGroup { Count = 4, Allocation = WorkPool.Production });

        float baseProd = colony.EffectiveProductionOutput;
        colony.Buildings.Add("industrial_complex"); // +25% production

        float boostedProd = colony.EffectiveProductionOutput;
        Assert.True(boostedProd > baseProd);
        Assert.InRange(boostedProd, baseProd * 1.2f, baseProd * 1.3f);
    }

    [Fact]
    public void HappinessModifier_FullAt70()
    {
        var colony = MakeColony();
        colony.Happiness = 70f;
        Assert.Equal(1.0f, colony.HappinessModifier);

        colony.Happiness = 100f;
        Assert.Equal(1.0f, colony.HappinessModifier);
    }

    [Fact]
    public void HappinessModifier_ReducedBelow70()
    {
        var colony = MakeColony();
        colony.Happiness = 35f;
        float mod = colony.HappinessModifier;
        Assert.True(mod < 1.0f);
        Assert.True(mod > 0.3f);
    }

    [Fact]
    public void HappinessModifier_MinAt0()
    {
        var colony = MakeColony();
        colony.Happiness = 0f;
        Assert.Equal(0.3f, colony.HappinessModifier);
    }

    [Fact]
    public void ExpertSlots_FromBuildings()
    {
        var colony = MakeColony();
        Assert.Equal(0, colony.ExpertSlots);
        colony.Buildings.Add("industrial_complex");
        Assert.Equal(1, colony.ExpertSlots);
    }
}
