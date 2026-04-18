namespace DerlictEmpires.Core.Enums;

/// <summary>
/// Per-empire per-site activity state. A site-level toggle — not a fleet order.
/// Any capable in-system fleets owned by the empire automatically contribute
/// to active activities; capacity splits evenly across multiple active sites
/// of the same type.
/// </summary>
public enum SiteActivity
{
    None,
    Scanning,
    Extracting
}
