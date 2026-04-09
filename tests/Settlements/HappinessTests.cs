using Xunit;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;

namespace DerlictEmpires.Tests.Settlements;

public class HappinessTests
{
    private static Colony MakeColony(int foodWorkers = 2, int totalPop = 3)
    {
        var colony = new Colony
        {
            Id = 0, Name = "Happiness Test", PlanetSize = PlanetSize.Medium
        };
        if (foodWorkers > 0)
            colony.PopGroups.Add(new PopGroup { Count = foodWorkers, Allocation = WorkPool.Food });
        if (totalPop - foodWorkers > 0)
            colony.PopGroups.Add(new PopGroup { Count = totalPop - foodWorkers, Allocation = WorkPool.Production });
        return colony;
    }

    [Fact]
    public void WellFed_Colony_HasHighHappiness()
    {
        var colony = MakeColony(foodWorkers: 3, totalPop: 3);
        float happiness = HappinessCalculator.Calculate(colony);
        Assert.True(happiness >= 60f, $"Well-fed colony happiness {happiness} should be >= 60");
    }

    [Fact]
    public void Starving_Colony_HasLowHappiness()
    {
        var colony = MakeColony(foodWorkers: 0, totalPop: 5);
        float happiness = HappinessCalculator.Calculate(colony);
        Assert.True(happiness < 40f, $"Starving colony happiness {happiness} should be < 40");
    }

    [Fact]
    public void Entertainment_BoostsHappiness()
    {
        var colony = MakeColony(foodWorkers: 2, totalPop: 3);
        float base_h = HappinessCalculator.Calculate(colony);

        colony.Buildings.Add("entertainment_center");
        float boosted_h = HappinessCalculator.Calculate(colony);

        Assert.True(boosted_h > base_h, "Entertainment center should boost happiness");
    }

    [Fact]
    public void Overcrowding_ReducesHappiness()
    {
        var colony = MakeColony(foodWorkers: 2, totalPop: 3);
        colony.PopGroups.Clear();
        // Way over cap
        colony.PopGroups.Add(new PopGroup { Count = colony.PopCap + 5, Allocation = WorkPool.Food });
        float happiness = HappinessCalculator.Calculate(colony);

        var normalColony = MakeColony(foodWorkers: 2, totalPop: 3);
        float normalH = HappinessCalculator.Calculate(normalColony);

        Assert.True(happiness < normalH, "Overcrowded colony should be less happy");
    }

    [Fact]
    public void Threat_ReducesHappiness()
    {
        var colony = MakeColony(foodWorkers: 2, totalPop: 3);
        float noThreat = HappinessCalculator.Calculate(colony, threatLevel: 0f);
        float highThreat = HappinessCalculator.Calculate(colony, threatLevel: 3f);

        Assert.True(highThreat < noThreat, "Threats should reduce happiness");
    }

    [Fact]
    public void Happiness_ClampedTo0And100()
    {
        var colony = MakeColony(foodWorkers: 0, totalPop: 50);
        float h = HappinessCalculator.Calculate(colony, threatLevel: 10f);
        Assert.InRange(h, 0f, 100f);

        var happyColony = MakeColony(foodWorkers: 5, totalPop: 5);
        happyColony.Buildings.Add("entertainment_center");
        happyColony.Buildings.Add("entertainment_center");
        happyColony.Buildings.Add("entertainment_center");
        float h2 = HappinessCalculator.Calculate(happyColony);
        Assert.InRange(h2, 0f, 100f);
    }
}
