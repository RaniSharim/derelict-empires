namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// Limited settlement on any POI. 3-5 pop cap, no buildings except
/// mining/salvage/defense, no population growth.
/// </summary>
public class Outpost : Settlement
{
    /// <summary>What this outpost is exploiting at its POI.</summary>
    public string ExploitationType { get; set; } = "Mining";

    /// <summary>Fixed pop cap for outposts.</summary>
    public int MaxPops { get; set; } = 4;

    public override int PopCap => MaxPops;
}
