using System.Collections.Generic;

namespace DerlictEmpires.Core.Ships;

/// <summary>
/// Hardcoded ship design templates for the MVP salvage loop.
/// Scout and Salvager are the only two ship types the first playable slice uses.
/// Later this moves to data/ship_types.json.
/// </summary>
public static class MvpShipDesigns
{
    public const string ScoutId = "scout_mk1";
    public const string SalvagerId = "salvager_mk1";

    public static readonly ShipDesign Scout = new()
    {
        Id = ScoutId,
        Name = "Scout Mk.I",
        ChassisId = "corvette_fast",
        ScanStrength = 10f,
        ExtractionStrength = 1f,
        Speed = 18f,
    };

    public static readonly ShipDesign Salvager = new()
    {
        Id = SalvagerId,
        Name = "Salvager Mk.I",
        ChassisId = "frigate_fast",
        ScanStrength = 2f,
        ExtractionStrength = 15f,
        Speed = 14f,
    };

    public static IReadOnlyDictionary<string, ShipDesign> Registry { get; } = new Dictionary<string, ShipDesign>
    {
        [ScoutId] = Scout,
        [SalvagerId] = Salvager,
    };
}
