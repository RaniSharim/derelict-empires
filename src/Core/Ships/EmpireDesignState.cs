using System.Collections.Generic;
using System.Linq;

namespace DerlictEmpires.Core.Ships;

/// <summary>
/// Per-empire persistent collection of saved ship designs and fleet templates.
/// Hangs off <see cref="DerlictEmpires.Core.Models.EmpireData"/>; serialized via GameSaveData.
/// </summary>
public class EmpireDesignState
{
    public List<ShipDesign> Designs { get; set; } = new();
    public List<FleetTemplate> Templates { get; set; } = new();

    /// <summary>Monotonic counters so generated IDs are stable across a save/load round trip.</summary>
    public int NextDesignIndex { get; set; } = 1;
    public int NextTemplateIndex { get; set; } = 1;

    // === Designs ===

    /// <summary>Create a new design with a generated ID, add it to the state, return it.</summary>
    public ShipDesign CreateDesign(string chassisId, string? name = null)
    {
        var design = new ShipDesign
        {
            Id = $"design_{NextDesignIndex++}",
            Name = name ?? $"{chassisId} Draft",
            ChassisId = chassisId,
        };
        Designs.Add(design);
        return design;
    }

    /// <summary>Commit an already-constructed design (e.g. an overlay's draft). Assigns an ID if missing.</summary>
    public void AddDesign(ShipDesign design)
    {
        if (string.IsNullOrEmpty(design.Id))
            design.Id = $"design_{NextDesignIndex++}";
        Designs.Add(design);
    }

    public bool RemoveDesign(string id) => Designs.RemoveAll(d => d.Id == id) > 0;

    public ShipDesign? GetDesign(string id) => Designs.FirstOrDefault(d => d.Id == id);

    // === Templates ===

    public FleetTemplate CreateTemplate(string? name = null)
    {
        var tmpl = new FleetTemplate
        {
            Id = $"template_{NextTemplateIndex++}",
            Name = name ?? "New Template",
        };
        Templates.Add(tmpl);
        return tmpl;
    }

    public void AddTemplate(FleetTemplate template)
    {
        if (string.IsNullOrEmpty(template.Id))
            template.Id = $"template_{NextTemplateIndex++}";
        Templates.Add(template);
    }

    public bool RemoveTemplate(string id) => Templates.RemoveAll(t => t.Id == id) > 0;

    public FleetTemplate? GetTemplate(string id) => Templates.FirstOrDefault(t => t.Id == id);

    /// <summary>Find every template that references the given design id.</summary>
    public List<FleetTemplate> TemplatesUsingDesign(string designId) =>
        Templates.Where(t => t.Entries.Any(e => e.DesignId == designId)).ToList();
}
