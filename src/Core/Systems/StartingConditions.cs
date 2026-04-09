using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Defines starting assets per origin template (DESIGN.md §4.2-4.3).
/// All players start with: home colony, station with shipyard, 1 Scout, 1 Fighter, 1 Salvager, 1 Builder, + 1 bonus ship from origin.
/// </summary>
public static class StartingConditions
{
    public class StartingAssets
    {
        public List<ShipTemplate> Ships { get; set; } = new();
        public bool CanSeeHiddenLanes { get; set; }
        public int StartingVisibilityHops { get; set; } = 1;
        public float ResearchSpeedBonus { get; set; }
        public float CombatBonus { get; set; }
        public float MaintenanceBonus { get; set; }
        public float ColorResearchBonus { get; set; }

        /// <summary>Starting stockpile of affinity-color resources.</summary>
        public float StartingSimpleEnergy { get; set; } = 200f;
        public float StartingSimpleParts { get; set; } = 200f;
        public float StartingAdvancedEnergy { get; set; } = 20f;
        public float StartingAdvancedParts { get; set; } = 20f;
        public long StartingCredits { get; set; } = 500;
    }

    public class ShipTemplate
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public ShipSizeClass SizeClass { get; set; }
        public int Hp { get; set; } = 100;
    }

    /// <summary>Base ships every origin gets.</summary>
    private static readonly ShipTemplate[] BaseShips =
    {
        new() { Name = "Scout", Role = "Scout", SizeClass = ShipSizeClass.Corvette, Hp = 60 },
        new() { Name = "Fighter", Role = "Fighter", SizeClass = ShipSizeClass.Fighter, Hp = 40 },
        new() { Name = "Salvager", Role = "Salvager", SizeClass = ShipSizeClass.Frigate, Hp = 80 },
        new() { Name = "Builder", Role = "Builder", SizeClass = ShipSizeClass.Corvette, Hp = 70 },
    };

    public static StartingAssets GetForOrigin(Origin origin)
    {
        var assets = new StartingAssets();
        assets.Ships.AddRange(BaseShips);

        switch (origin)
        {
            case Origin.Warriors:
                assets.Ships.Add(new ShipTemplate
                    { Name = "Assault Fighter", Role = "Fighter", SizeClass = ShipSizeClass.Fighter, Hp = 50 });
                assets.CombatBonus = 0.10f;
                break;

            case Origin.Servitors:
                assets.Ships.Add(new ShipTemplate
                    { Name = "Survey Salvager", Role = "Salvager", SizeClass = ShipSizeClass.Frigate, Hp = 80 });
                assets.ResearchSpeedBonus = 0.10f;
                assets.MaintenanceBonus = -0.10f;
                break;

            case Origin.Haulers:
                assets.Ships.Add(new ShipTemplate
                    { Name = "Pathfinder", Role = "Scout", SizeClass = ShipSizeClass.Corvette, Hp = 60 });
                assets.CanSeeHiddenLanes = true;
                assets.StartingVisibilityHops = 2;
                break;

            case Origin.Chroniclers:
                assets.Ships.Add(new ShipTemplate
                    { Name = "Archival Scout", Role = "Scout", SizeClass = ShipSizeClass.Corvette, Hp = 60 });
                assets.ColorResearchBonus = 0.15f;
                break;

            case Origin.FreeRace:
                assets.Ships.Add(new ShipTemplate
                    { Name = "Pioneer Builder", Role = "Builder", SizeClass = ShipSizeClass.Corvette, Hp = 70 });
                break;
        }

        return assets;
    }
}
