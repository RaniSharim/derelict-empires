using Xunit;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Tests.Ships;

public class ChassisDataTests
{
    [Fact]
    public void All14Chassis_Exist()
    {
        Assert.Equal(14, ChassisData.All.Length);
    }

    [Fact]
    public void Each_SizeClass_Has2Variants()
    {
        foreach (var size in System.Enum.GetValues<ShipSizeClass>())
        {
            var variants = ChassisData.GetBySize(size);
            Assert.Equal(2, variants.Count);
        }
    }

    [Fact]
    public void AllIds_Unique()
    {
        var ids = new HashSet<string>();
        foreach (var c in ChassisData.All)
            Assert.True(ids.Add(c.Id), $"Duplicate chassis ID: {c.Id}");
    }

    [Fact]
    public void FindById_Works()
    {
        var chassis = ChassisData.FindById("corvette_fast");
        Assert.NotNull(chassis);
        Assert.Equal(ShipSizeClass.Corvette, chassis.SizeClass);
    }
}

public class ShipDesignValidatorTests
{
    [Fact]
    public void ValidDesign_Passes()
    {
        var design = new ShipDesign
        {
            Id = "test", Name = "Test Ship", ChassisId = "corvette_fast",
            SlotFills = new() { "sub_a", "sub_b" }
        };
        var result = ShipDesignValidator.Validate(design);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UnknownChassis_Fails()
    {
        var design = new ShipDesign { ChassisId = "nonexistent" };
        var result = ShipDesignValidator.Validate(design);
        Assert.False(result.IsValid);
        Assert.Contains("Unknown chassis", result.Error);
    }

    [Fact]
    public void TooManySlots_Fails()
    {
        // Corvette fast has 2 big system slots
        var design = new ShipDesign
        {
            ChassisId = "corvette_fast",
            SlotFills = new() { "a", "b", "c" } // 3 > 2
        };
        var result = ShipDesignValidator.Validate(design);
        Assert.False(result.IsValid);
        Assert.Contains("Too many slot fills", result.Error);
    }

    [Fact]
    public void OverCapacity_Fails()
    {
        // Corvette fast has 5 free capacity, each extra uses 2
        var design = new ShipDesign
        {
            ChassisId = "corvette_fast",
            SlotFills = new() { "a" },
            Extras = new() { "e1", "e2", "e3" } // 6 > 5
        };
        var result = ShipDesignValidator.Validate(design);
        Assert.False(result.IsValid);
        Assert.Contains("Over capacity", result.Error);
    }

    [Fact]
    public void UnresearchedSubsystem_Fails()
    {
        var researched = new HashSet<string> { "sub_a" };
        var design = new ShipDesign
        {
            ChassisId = "corvette_fast",
            SlotFills = new() { "sub_a", "sub_unknown" }
        };
        var result = ShipDesignValidator.Validate(design, researched);
        Assert.False(result.IsValid);
        Assert.Contains("Unresearched", result.Error);
    }

    [Fact]
    public void EmptySlots_Allowed()
    {
        var design = new ShipDesign
        {
            ChassisId = "destroyer_balanced",
            SlotFills = new() { "a", "", "" } // 2 empty slots OK
        };
        var result = ShipDesignValidator.Validate(design);
        Assert.True(result.IsValid);
    }
}

public class ShipStatCalculatorTests
{
    [Fact]
    public void Stats_MatchChassis_WithNoSlots()
    {
        var design = new ShipDesign { ChassisId = "corvette_fast" };
        var stats = ShipStatCalculator.Calculate(design);

        var chassis = ChassisData.FindById("corvette_fast")!;
        Assert.Equal(chassis.BaseHp, stats.Hp);
        Assert.Equal(chassis.BaseSpeed, stats.Speed);
    }

    [Fact]
    public void FilledSlots_IncreaseVisibility()
    {
        var noSlots = new ShipDesign { ChassisId = "corvette_fast" };
        var withSlots = new ShipDesign
        {
            ChassisId = "corvette_fast",
            SlotFills = new() { "a", "b" }
        };

        var statsNone = ShipStatCalculator.Calculate(noSlots);
        var statsFilled = ShipStatCalculator.Calculate(withSlots);

        Assert.True(statsFilled.Visibility > statsNone.Visibility);
    }

    [Fact]
    public void ExpertiseBonus_ScalesHp()
    {
        var design = new ShipDesign { ChassisId = "destroyer_balanced" };
        var normal = ShipStatCalculator.Calculate(design, 1.0f);
        var expert = ShipStatCalculator.Calculate(design, 1.2f);

        Assert.True(expert.Hp > normal.Hp);
    }
}
