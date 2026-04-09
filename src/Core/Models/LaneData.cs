using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>A navigable connection between two star systems.</summary>
public class LaneData
{
    public int SystemA { get; set; }
    public int SystemB { get; set; }
    public LaneType Type { get; set; } = LaneType.Visible;
    public float Distance { get; set; }

    /// <summary>Whether this lane is a strategic chokepoint.</summary>
    public bool IsChokepoint { get; set; }

    public int GetOtherSystem(int systemId) =>
        systemId == SystemA ? SystemB : SystemA;

    public bool Connects(int systemId) =>
        systemId == SystemA || systemId == SystemB;
}
