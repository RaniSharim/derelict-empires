namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Authored danger that may trigger when scavenging a layer. Loaded from
/// <c>resources/data/salvage_dangers.json</c>. Initial catalog has just
/// <c>damage</c>; the schema admits future kinds (sabotage, contamination, ...).
/// </summary>
public class SalvageDangerTypeDef
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>Discriminator for the runtime resolver. Today: <c>FleetDamage</c>.</summary>
    public string EffectKind { get; set; } = "FleetDamage";

    public float BaseSeverity { get; set; } = 5f;
    public float PerTierBonus { get; set; } = 3f;
}
