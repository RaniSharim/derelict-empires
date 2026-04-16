namespace DerlictEmpires.Core.Enums;

/// <summary>
/// Kept for backward compatibility during migration.
/// V2 merges components into ResourceType (BasicComponent, AdvancedComponent).
/// Map: ComponentTier.Basic → ResourceType.BasicComponent,
///      ComponentTier.Advanced → ResourceType.AdvancedComponent.
/// </summary>
[System.Obsolete("Use ResourceType.BasicComponent / AdvancedComponent instead")]
public enum ComponentTier
{
    Basic,
    Advanced
}
