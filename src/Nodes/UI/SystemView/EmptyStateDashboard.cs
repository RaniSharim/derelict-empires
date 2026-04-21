using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right panel empty-state "situation" dashboard. Shown when no entity is selected.
/// Lists foreign presence, player running ops, and recent events (v3 placeholders).
/// See design/in_system_design.md §7.2.
/// </summary>
public partial class EmptyStateDashboard : VBoxContainer
{
    public void Populate(
        StarSystemData? system,
        IReadOnlyList<Colony>? colonies,
        IReadOnlyList<Outpost>? outposts,
        IReadOnlyList<StationData>? stations,
        int viewerEmpireId)
    {
        foreach (var child in GetChildren()) child.QueueFree();
        AddThemeConstantOverride("separation", 10);

        AddHeader("SITUATION · no selection · overview");

        // Foreign Presence.
        var foreignColonies = colonies?.Where(c => c.SystemId == system?.Id && c.OwnerEmpireId != viewerEmpireId).ToList() ?? new List<Colony>();
        var foreignOutposts = outposts?.Where(o => o.SystemId == system?.Id && o.OwnerEmpireId != viewerEmpireId).ToList() ?? new List<Outpost>();
        var foreignStations = stations?.Where(s => s.SystemId == system?.Id && s.OwnerEmpireId != viewerEmpireId).ToList() ?? new List<StationData>();

        AddSection("FOREIGN PRESENCE");
        int foreignCount = foreignColonies.Count + foreignOutposts.Count + foreignStations.Count;
        if (foreignCount == 0)
        {
            AddBody("—  none detected");
        }
        else
        {
            foreach (var c in foreignColonies) AddBody($"colony · {c.Name}");
            foreach (var o in foreignOutposts) AddBody($"outpost · {o.Name}");
            foreach (var s in foreignStations) AddBody($"station · {s.Name}");
        }

        // Running Ops (placeholder in v3 — full wiring once extract/construction signals feed here).
        AddSection("RUNNING OPS");
        AddBody("—  nothing active");

        // Recent events.
        AddSection("RECENT");
        AddBody("system view opened");
    }

    private void AddHeader(string text)
    {
        var l = new Label { Text = text };
        UIFonts.Style(l, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
        AddChild(l);
    }

    private void AddSection(string title)
    {
        var l = new Label { Text = title };
        UIFonts.Style(l, UIFonts.Main, 10, UIColors.TextFaint);
        AddChild(l);
    }

    private void AddBody(string text)
    {
        var l = new Label { Text = text };
        UIFonts.Style(l, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        AddChild(l);
    }
}
