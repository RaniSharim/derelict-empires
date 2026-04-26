using System.Collections.Generic;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Authored "fix it" outcome unlocked once a site's deepest layer is revealed.
/// Loaded from <c>resources/data/salvage_outcomes.json</c>. Resolved at
/// runtime by <c>SalvageOutcomeProcessor</c> in phase 6.
/// </summary>
public class SalvageOutcomeDef
{
    public string Id { get; set; } = "";

    /// <summary>Discriminator for the resolver. Today: <c>RepairStation</c>, <c>RecoverDerelict</c>.</summary>
    public string Action { get; set; } = "";

    /// <summary>
    /// Cost paid from the empire stockpile. Keys are either fully qualified
    /// (<c>Red_BasicComponent</c>) or bare component types (<c>BasicComponent</c>),
    /// in which case the runtime resolves them against the site's primary color.
    /// </summary>
    public Dictionary<string, float> Cost { get; set; } = new();

    /// <summary>Free-form parameter blob consumed by the action resolver.</summary>
    public Dictionary<string, string> Params { get; set; } = new();
}
