using System;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Settlements;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// One row in the Buildings · Pops list. At-rest = 24px compact row; focused = ~60px with
/// clickable slot chips + UPG / SCRAP actions. Clicking the row header toggles focus.
/// See design/in_system_design.md §8.2.
/// </summary>
public partial class BuildingRow : VBoxContainer
{
    public Colony Colony { get; }
    public BuildingData Building { get; }

    /// <summary>Event fired when focus state changes; panel uses this to collapse siblings.</summary>
    public Action<BuildingRow>? OnFocusRequested;

    private bool _focused;
    private PanelContainer _outer = null!;
    private StyleBoxFlat _idleBg = null!;
    private StyleBoxFlat _focusedBg = null!;

    public BuildingRow(Colony colony, BuildingData building)
    {
        Colony = colony;
        Building = building;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 0);
        _outer = new PanelContainer();
        _idleBg = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2 };
        _focusedBg = new StyleBoxFlat
        {
            BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.06f),
            BorderColor = UIColors.BorderDim,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 4, ContentMarginBottom = 4,
        };
        _outer.AddThemeStyleboxOverride("panel", _idleBg);
        _outer.MouseFilter = Control.MouseFilterEnum.Stop;
        _outer.GuiInput += OnOuterGuiInput;
        _outer.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        AddChild(_outer);

        Rebuild();
    }

    private void OnOuterGuiInput(InputEvent e)
    {
        // Click anywhere on the row header (except over a chip or action button) toggles focus.
        // Chips set MouseFilter=Stop on themselves, so their clicks are consumed before reaching here.
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            OnFocusRequested?.Invoke(this);
        }
    }

    public void SetFocused(bool focused)
    {
        if (_focused == focused) return;
        _focused = focused;
        _outer.AddThemeStyleboxOverride("panel", _focused ? _focusedBg : _idleBg);
        Rebuild();
    }

    public void Rebuild()
    {
        foreach (var child in _outer.GetChildren()) child.QueueFree();

        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 2);
        _outer.AddChild(v);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 6);
        v.AddChild(header);

        var pool = PoolFor(Building);
        int expectedSlots = WorkerSlots(Building);
        int expertSlots   = Building.ExpertSlots;
        int filled        = pool.HasValue ? Math.Min(Colony.GetWorkersIn(pool.Value), expectedSlots) : 0;

        // Building name.
        var nameLbl = new Label { Text = Building.DisplayName };
        UIFonts.Style(nameLbl, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        nameLbl.CustomMinimumSize = new Vector2(100, 0);
        header.AddChild(nameLbl);

        // Inline slot row (compact).
        if (!_focused)
        {
            var chipBox = new HBoxContainer();
            chipBox.AddThemeConstantOverride("separation", 2);
            for (int i = 0; i < expectedSlots; i++)
            {
                var chip = new Label { Text = i < filled ? "●" : "·" };
                UIFonts.Style(chip, UIFonts.Main, UIFonts.SmallSize,
                    i < filled ? UIColors.SigIcon : UIColors.TextFaint);
                chipBox.AddChild(chip);
            }
            for (int i = 0; i < expertSlots; i++)
            {
                var chip = new Label { Text = "◇" }; // expert always empty in v1 — no per-building expert assignment logic yet
                UIFonts.Style(chip, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
                chipBox.AddChild(chip);
            }
            header.AddChild(chipBox);
        }

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Trailing output tag.
        var outTag = new Label { Text = OutputTag(Building) };
        UIFonts.Style(outTag, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
        header.AddChild(outTag);

        // Whole-row click-to-focus is handled by OnOuterGuiInput on the enclosing PanelContainer.

        if (_focused)
        {
            // Expanded chip row + UPG / SCRAP buttons.
            var editRow = new HBoxContainer();
            editRow.AddThemeConstantOverride("separation", 4);
            v.AddChild(editRow);

            for (int i = 0; i < expectedSlots; i++)
            {
                int capturedIdx = i;
                bool isFilled = i < filled;
                var chip = new SlotChip(SlotChip.Kind.Worker, isFilled, capturedIdx);
                chip.OnClicked = () => ToggleSlot(capturedIdx, !isFilled, pool);
                editRow.AddChild(chip);
            }
            for (int i = 0; i < expertSlots; i++)
            {
                var chip = new SlotChip(SlotChip.Kind.Expert, false, expectedSlots + i);
                chip.OnClicked = () => { }; // expert toggling deferred
                editRow.AddChild(chip);
            }

            editRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

            foreach (var label in new[] { "UPG", "SCRAP" })
            {
                var b = new Button { Text = label, Flat = true };
                UIFonts.StyleButtonRole(b, UIFonts.Role.Small, UIColors.TextDim);
                editRow.AddChild(b);
            }
        }
    }

    private void ToggleSlot(int slotIndex, bool newFilled, WorkPool? pool)
    {
        if (!pool.HasValue) return;
        // Slot edits move pops between the building pool and the Unassigned (idle) bucket.
        // Taking from other buildings would silently defund them; idle pops are the explicit
        // reserve. If the reserve is empty, fill is a no-op — player must free a pop first.
        if (newFilled)
        {
            var idle = FindOrNull(Colony, WorkPool.Unassigned);
            if (idle == null || idle.Count <= 0) return;
            MovePop(Colony, WorkPool.Unassigned, pool.Value);
        }
        else
        {
            MovePop(Colony, pool.Value, WorkPool.Unassigned);
        }
        EventBus.Instance?.FireBuildingSlotToggled(Colony.Id, Building.Id, slotIndex, newFilled);
        EventBus.Instance?.FireColonyPopsChanged(Colony.Id);
    }

    private static void MovePop(Colony colony, WorkPool from, WorkPool to)
    {
        var src = FindOrNull(colony, from);
        if (src == null || src.Count <= 0) return;
        src.Count--;
        if (src.Count == 0) colony.PopGroups.Remove(src);

        var dst = FindOrNull(colony, to);
        if (dst == null)
        {
            colony.PopGroups.Add(new PopGroup { Count = 1, Allocation = to });
        }
        else dst.Count++;
    }

    private static PopGroup? FindOrNull(Colony colony, WorkPool pool)
    {
        foreach (var g in colony.PopGroups)
            if (g.Allocation == pool) return g;
        return null;
    }

    private static WorkPool? PoolFor(BuildingData b)
    {
        if (b.MiningBonus    > 0) return WorkPool.Mining;
        if (b.ResearchBonus  > 0) return WorkPool.Research;
        if (b.FoodBonus      > 0) return WorkPool.Food;
        if (b.ProductionBonus > 0) return WorkPool.Production;
        return null; // decorative buildings (defense, entertainment, hab, logistics)
    }

    private static int WorkerSlots(BuildingData b) =>
        PoolFor(b).HasValue ? 4 : 0;

    private static string OutputTag(BuildingData b)
    {
        if (b.MiningBonus     > 0) return $"+mine {b.MiningBonus:P0}";
        if (b.ResearchBonus   > 0) return $"+res {b.ResearchBonus:P0}";
        if (b.FoodBonus       > 0) return $"+food {b.FoodBonus:P0}";
        if (b.ProductionBonus > 0) return $"+prod {b.ProductionBonus:P0}";
        if (b.BonusPopCap     > 0) return $"+cap {b.BonusPopCap}";
        if (b.HappinessBonus  > 0) return $"+happy {b.HappinessBonus:F0}";
        if (b.DefenseStrength > 0) return $"+def {b.DefenseStrength:F0}";
        if (b.LogisticsCapacity > 0) return $"+log {b.LogisticsCapacity:F0}";
        return "";
    }
}
