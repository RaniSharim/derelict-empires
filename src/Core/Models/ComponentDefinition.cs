using System.Linq;
using DerlictEmpires.Core.Enums;

#pragma warning disable CS0618 // ComponentTier still used here as canonical type
namespace DerlictEmpires.Core.Models;

/// <summary>
/// Static definition of a component type. 10 total (5 colors × 2 tiers).
/// </summary>
public class ComponentDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public PrecursorColor Color { get; set; }
    public ComponentTier Tier { get; set; }

    /// <summary>All 10 component definitions.</summary>
    public static readonly ComponentDefinition[] All = GenerateAll();

    private static ComponentDefinition[] GenerateAll()
    {
        return new[]
        {
            new ComponentDefinition { Id = "basic_red", DisplayName = "Basic Red Component", Color = PrecursorColor.Red, Tier = ComponentTier.Basic },
            new ComponentDefinition { Id = "advanced_red", DisplayName = "Advanced Red Component", Color = PrecursorColor.Red, Tier = ComponentTier.Advanced },
            new ComponentDefinition { Id = "basic_blue", DisplayName = "Basic Blue Component", Color = PrecursorColor.Blue, Tier = ComponentTier.Basic },
            new ComponentDefinition { Id = "advanced_blue", DisplayName = "Advanced Blue Component", Color = PrecursorColor.Blue, Tier = ComponentTier.Advanced },
            new ComponentDefinition { Id = "basic_green", DisplayName = "Basic Green Component", Color = PrecursorColor.Green, Tier = ComponentTier.Basic },
            new ComponentDefinition { Id = "advanced_green", DisplayName = "Advanced Green Component", Color = PrecursorColor.Green, Tier = ComponentTier.Advanced },
            new ComponentDefinition { Id = "basic_gold", DisplayName = "Basic Gold Component", Color = PrecursorColor.Gold, Tier = ComponentTier.Basic },
            new ComponentDefinition { Id = "advanced_gold", DisplayName = "Advanced Gold Component", Color = PrecursorColor.Gold, Tier = ComponentTier.Advanced },
            new ComponentDefinition { Id = "basic_purple", DisplayName = "Basic Purple Component", Color = PrecursorColor.Purple, Tier = ComponentTier.Basic },
            new ComponentDefinition { Id = "advanced_purple", DisplayName = "Advanced Purple Component", Color = PrecursorColor.Purple, Tier = ComponentTier.Advanced },
        };
    }

    public static ComponentDefinition? Find(PrecursorColor color, ComponentTier tier) =>
        All.FirstOrDefault(c => c.Color == color && c.Tier == tier);
}
