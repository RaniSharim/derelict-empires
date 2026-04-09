using System;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Tracks what the player has selected (fleet or system). Pure C#.
/// Only one thing can be selected at a time.
/// </summary>
public class SelectionManager
{
    public enum SelectionType { None, System, Fleet }

    public SelectionType CurrentType { get; private set; } = SelectionType.None;
    public int SelectedId { get; private set; } = -1;

    public event Action<SelectionType, int>? SelectionChanged;

    public void SelectSystem(int systemId)
    {
        CurrentType = SelectionType.System;
        SelectedId = systemId;
        SelectionChanged?.Invoke(CurrentType, SelectedId);
    }

    public void SelectFleet(int fleetId)
    {
        CurrentType = SelectionType.Fleet;
        SelectedId = fleetId;
        SelectionChanged?.Invoke(CurrentType, SelectedId);
    }

    public void Deselect()
    {
        CurrentType = SelectionType.None;
        SelectedId = -1;
        SelectionChanged?.Invoke(CurrentType, SelectedId);
    }

    public bool IsFleetSelected => CurrentType == SelectionType.Fleet;
    public bool IsSystemSelected => CurrentType == SelectionType.System;
}
