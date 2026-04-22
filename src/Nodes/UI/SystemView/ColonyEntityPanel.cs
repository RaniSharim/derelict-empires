using System.Collections.Generic;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Settlements;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right-panel variant shown when a colony is the Selected Entity.
/// Header + status + Buildings·Pops (completed rows with slot chips, under-construction rows
/// with progress, + ADD terminator) + Detection + Actions. See design/in_system_design.md §8.
/// </summary>
public partial class ColonyEntityPanel : VBoxContainer
{
    private Colony? _colony;
    private VBoxContainer? _buildingsList;
    private readonly List<BuildingRow> _buildingRows = new();
    private BuildingRow? _focusedRow;

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.ColonyPopsChanged -= OnColonyPopsChanged;
    }

    public void Populate(Colony colony)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        _buildingRows.Clear();
        _focusedRow = null;
        AddThemeConstantOverride("separation", 8);

        _colony = colony;
        if (colony == null) return;

        if (EventBus.Instance != null)
        {
            EventBus.Instance.ColonyPopsChanged -= OnColonyPopsChanged;
            EventBus.Instance.ColonyPopsChanged += OnColonyPopsChanged;
        }

        // Header row.
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        AddChild(headerRow);

        var accent = new ColorRect
        {
            Color = new Color("#22dd44"),
            CustomMinimumSize = new Vector2(3, 20),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        headerRow.AddChild(accent);

        var name = new Label { Text = string.IsNullOrEmpty(colony.Name) ? $"Colony {colony.Id}" : colony.Name };
        UIFonts.Style(name, UIFonts.Title, 13, UIColors.TextBright);
        headerRow.AddChild(name);

        headerRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        headerRow.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, (colony.TotalPopulation * 6).ToString()));
        headerRow.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Sensor, 11, "0"));

        // Status row.
        int idle = colony.GetWorkersIn(WorkPool.Unassigned);
        var statusLine = new Label
        {
            Text = $"POPS {colony.TotalPopulation}/{colony.PopCap} · IDLE {idle} · HAPPY {(int)colony.Happiness} · PRIO: {PriorityLabel(colony.Priority)}",
        };
        UIFonts.Style(statusLine, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        AddChild(statusLine);

        // Buildings·Pops section (unified).
        AddSection("BUILDINGS · POPS");
        _buildingsList = new VBoxContainer();
        _buildingsList.AddThemeConstantOverride("separation", 2);
        AddChild(_buildingsList);
        RebuildBuildingsList();

        // Detection block.
        AddSection("DETECTION");
        AddBody($"sig sources · pops  |  range 1b  |  observers none");

        // Actions.
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        AddChild(actions);
        foreach (var label in new[] { "CLAIM", "GOV ▾", "DISPATCH" })
        {
            var b = new Button { Text = label, Flat = true };
            UIFonts.StyleButtonRole(b, UIFonts.Role.Small, UIColors.TextDim);
            actions.AddChild(b);
        }
    }

    private void OnColonyPopsChanged(int colonyId)
    {
        if (_colony == null || _colony.Id != colonyId) return;
        RebuildBuildingsList();
    }

    private void RebuildBuildingsList()
    {
        if (_buildingsList == null || _colony == null) return;
        foreach (var child in _buildingsList.GetChildren()) child.QueueFree();
        _buildingRows.Clear();
        int focusedBuildingId = -1;
        var focusedId = _focusedRow?.Building.Id;
        _focusedRow = null;

        // Completed buildings.
        foreach (var buildingId in _colony.Buildings)
        {
            var data = BuildingData.FindById(buildingId);
            if (data == null)
            {
                AddBody($"●  {buildingId} (unknown)");
                continue;
            }
            var row = new BuildingRow(_colony, data);
            row.OnFocusRequested = RequestFocus;
            _buildingsList.AddChild(row);
            _buildingRows.Add(row);
            if (focusedId == data.Id) _focusedRow = row;
        }

        // Under-construction rows from the colony's queue.
        foreach (var entry in _colony.Queue.Entries)
        {
            string name = entry.Item?.DisplayName ?? "building";
            _buildingsList.AddChild(BuildConstructionRow(name, entry.Progress));
        }

        // + ADD terminator row.
        _buildingsList.AddChild(BuildAddRow());

        // Restore focus if the building still exists.
        if (_focusedRow != null) _focusedRow.SetFocused(true);
    }

    private void RequestFocus(BuildingRow row)
    {
        if (_focusedRow == row)
        {
            row.SetFocused(false);
            _focusedRow = null;
            return;
        }
        _focusedRow?.SetFocused(false);
        _focusedRow = row;
        row.SetFocused(true);
    }

    private Control BuildConstructionRow(string name, float progress)
    {
        var row = new HBoxContainer();
        row.CustomMinimumSize = new Vector2(0, 20);
        row.AddThemeConstantOverride("separation", 6);
        row.Modulate = new Color(1, 1, 1, 0.75f);

        var icon = new Label { Text = "◌" };
        UIFonts.Style(icon, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
        row.AddChild(icon);

        var label = new Label { Text = name, ClipText = true };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        label.CustomMinimumSize = new Vector2(100, 0);
        row.AddChild(label);

        // Inline progress bar — ColorRect ratio.
        var trackWrap = new PanelContainer { CustomMinimumSize = new Vector2(0, 6) };
        trackWrap.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        trackWrap.SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter;
        var trackBg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.15f, 0.6f) };
        trackWrap.AddThemeStyleboxOverride("panel", trackBg);
        var fill = new ColorRect { Color = UIColors.SigIcon };
        fill.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        fill.AnchorRight = Mathf.Clamp(progress, 0f, 1f);
        fill.MouseFilter = Control.MouseFilterEnum.Ignore;
        trackWrap.AddChild(fill);
        row.AddChild(trackWrap);

        var pct = new Label { Text = $"{(int)(progress * 100)}%" };
        UIFonts.Style(pct, UIFonts.Main, 10, UIColors.TextDim);
        pct.CustomMinimumSize = new Vector2(40, 0);
        pct.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(pct);

        return row;
    }

    private Control BuildAddRow()
    {
        var row = new HBoxContainer();
        row.CustomMinimumSize = new Vector2(0, 20);
        row.AddThemeConstantOverride("separation", 6);

        var add = new Button { Text = "+ ADD", Flat = true };
        UIFonts.StyleButtonRole(add, UIFonts.Role.Small, UIColors.Accent);
        add.MouseDefaultCursorShape = Control.CursorShape.PointingHand;

        var hint = new Label { Text = "build on empty slot" };
        UIFonts.Style(hint, UIFonts.Main, 10, UIColors.TextFaint);

        // PopupMenu of buildings not already built or queued. Select → enqueue + refresh.
        var popup = new PopupMenu();
        add.AddChild(popup);

        var availableIds = new List<string>();
        add.Pressed += () =>
        {
            if (_colony == null) return;
            popup.Clear();
            availableIds.Clear();

            var queuedIds = new HashSet<string>();
            foreach (var qe in _colony.Queue.Entries)
                if (qe.Item is BuildingProducible bp) queuedIds.Add(bp.Id);

            foreach (var b in BuildingData.All)
            {
                if (_colony.Buildings.Contains(b.Id)) continue;
                if (queuedIds.Contains(b.Id))        continue;
                availableIds.Add(b.Id);
                popup.AddItem($"{b.DisplayName}  ({b.ProductionCost})");
            }
            if (popup.ItemCount == 0)
            {
                popup.AddItem("— nothing to build —");
                popup.SetItemDisabled(0, true);
            }
            var pos = add.GetScreenPosition() + new Vector2(0, add.Size.Y);
            popup.Position = new Vector2I((int)pos.X, (int)pos.Y);
            popup.Popup();
        };

        popup.IndexPressed += (idx) =>
        {
            if (_colony == null) return;
            int i = (int)idx;
            if (i < 0 || i >= availableIds.Count) return;
            var def = BuildingData.FindById(availableIds[i]);
            if (def == null) return;
            _colony.Queue.Enqueue(new BuildingProducible(def));
            EventBus.Instance?.FireColonyPopsChanged(_colony.Id);
        };

        row.AddChild(add);
        row.AddChild(hint);
        return row;
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

    private static string PriorityLabel(ColonyPriority p) => p switch
    {
        ColonyPriority.Balanced        => "balanced",
        ColonyPriority.ProductionFocus => "prod",
        ColonyPriority.ResearchFocus   => "res",
        ColonyPriority.GrowthFocus     => "food",
        ColonyPriority.MiningFocus     => "mine",
        _                              => "—",
    };
}
