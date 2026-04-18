using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Payload for <see cref="EventBus.DesignerOpenRequested"/>. All fields optional — any null means "default/new".
/// </summary>
public class DesignerOpenRequest
{
    /// <summary>Open an existing design for editing. Null = create a new draft.</summary>
    public string? DesignId { get; set; }

    /// <summary>Open a fleet template for editing (template mode). Null = design mode.</summary>
    public string? TemplateId { get; set; }

    /// <summary>When opening a new draft, start with this chassis pre-selected.</summary>
    public string? ChassisId { get; set; }

    /// <summary>Scroll/focus the slot matrix to this slot when opening.</summary>
    public string? SlotId { get; set; }
}

/// <summary>
/// Payload for <see cref="EventBus.TechTreeOpenRequested"/>.
/// </summary>
public class TechTreeOpenRequest
{
    public PrecursorColor Color { get; set; }
    public TechCategory? Category { get; set; }
    public int? Tier { get; set; }
    public TechTreeIntent Intent { get; set; } = TechTreeIntent.View;

    /// <summary>Specific module/subsystem id to pre-select in the focus panel.</summary>
    public string? SubsystemId { get; set; }
}

/// <summary>Reason the Tech Tree overlay was opened — drives the primary action button label and node filtering.</summary>
public enum TechTreeIntent
{
    /// <summary>Default browse mode. Primary action = START TIER RESEARCH / START MODULE RESEARCH.</summary>
    View,
    /// <summary>Opened from a [+ ADD TIER] queue button. Only tier nodes selectable. Primary = QUEUE THIS TIER.</summary>
    QueueTier,
    /// <summary>Opened from a [+ ADD MODULE] queue button. Only module nodes selectable. Primary = QUEUE THIS MODULE.</summary>
    QueueModule,
    /// <summary>Opened from a designer [USE IN DESIGN] chip. Primary action jumps back to designer.</summary>
    UseInDesign,
}
