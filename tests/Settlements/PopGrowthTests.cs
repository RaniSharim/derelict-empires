using Xunit;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;

namespace DerlictEmpires.Tests.Settlements;

public class PopGrowthTests
{
    private static Colony MakeColony(int pop = 3, float happiness = 70f)
    {
        var colony = new Colony
        {
            Id = 0, Name = "Growth Test", PlanetSize = PlanetSize.Large,
            Happiness = happiness
        };
        colony.PopGroups.Add(new PopGroup { Count = pop, Allocation = WorkPool.Food });
        return colony;
    }

    [Fact]
    public void Growth_AccumulatesFoodSurplus()
    {
        var colony = MakeColony(3);
        // 3 pops in food → food output = 3 * 3 = 9. Consumed = 3. Surplus = 6 per tick.
        PopGrowthCalculator.ProcessTick(colony, 1.0f);
        Assert.True(colony.FoodSurplus > 0, "Food surplus should accumulate");
    }

    [Fact]
    public void Growth_TriggersAtThreshold()
    {
        var colony = MakeColony(3, 80f);
        int startPop = colony.TotalPopulation;

        // Keep ticking until growth happens
        bool grew = false;
        for (int i = 0; i < 100; i++)
        {
            if (PopGrowthCalculator.ProcessTick(colony, 1.0f))
            {
                grew = true;
                break;
            }
        }

        Assert.True(grew, "Colony should eventually grow");
        Assert.Equal(startPop + 1, colony.TotalPopulation);
    }

    [Fact]
    public void Growth_StopsAtPopCap()
    {
        var colony = MakeColony(3, 80f);
        // Set a tiny pop cap
        colony.PopGroups.Clear();
        colony.PopGroups.Add(new PopGroup { Count = colony.PopCap, Allocation = WorkPool.Food });

        bool grew = PopGrowthCalculator.ProcessTick(colony, 1.0f);
        Assert.False(grew, "Should not grow at pop cap");
    }

    [Fact]
    public void ZeroFood_NoGrowth()
    {
        var colony = MakeColony(0);
        colony.PopGroups.Clear();
        colony.PopGroups.Add(new PopGroup { Count = 3, Allocation = WorkPool.Production }); // No food workers

        int startPop = colony.TotalPopulation;
        PopGrowthCalculator.ProcessTick(colony, 1.0f);
        // Should not have gained any population (may have lost some to starvation)
        Assert.True(colony.TotalPopulation <= startPop, "No food workers means no growth");
    }

    [Fact]
    public void HighHappiness_GrowsFaster()
    {
        var colonyHappy = MakeColony(3, 90f);
        var colonySad = MakeColony(3, 30f);

        for (int i = 0; i < 10; i++)
        {
            PopGrowthCalculator.ProcessTick(colonyHappy, 1.0f);
            PopGrowthCalculator.ProcessTick(colonySad, 1.0f);
        }

        Assert.True(colonyHappy.FoodSurplus > colonySad.FoodSurplus,
            "Happy colony should accumulate food surplus faster");
    }
}
