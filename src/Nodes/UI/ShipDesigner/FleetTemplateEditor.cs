using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Nodes.Map;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Fleet Template editor — a variant of the designer focused on authoring fleet templates
/// rather than individual ship designs. Shows composition rows (design × count),
/// per-role disposition defaults, combined supply profile, and a footer listing fleets
/// that would auto-refit on save. Spec §3.9.
/// </summary>
public partial class FleetTemplateEditor : GlassOverlay
{
    private MainScene? _mainScene;
    private FleetTemplate _template = new();
    private bool _isNew = true;

    private LineEdit _nameEdit = null!;
    private VBoxContainer _compositionList = null!;
    private VBoxContainer _roleDefaultsList = null!;
    private HBoxContainer _supplyStrip = null!;
    private Label _footerLabel = null!;

    public FleetTemplateEditor()
    {
        OverlayTitle = "FLEET TEMPLATE";
    }

    public void Configure(MainScene mainScene, string? templateId)
    {
        _mainScene = mainScene;
        var player = mainScene.PlayerEmpire;
        if (player == null) return;

        if (!string.IsNullOrEmpty(templateId))
        {
            var existing = player.DesignState.GetTemplate(templateId!);
            if (existing != null)
            {
                _template = Clone(existing);
                _isNew = false;
                OverlayTitle = $"FLEET TEMPLATE: {existing.Name.ToUpperInvariant()}";
                return;
            }
        }

        _template = new FleetTemplate { Name = "New Template" };
        _isNew = true;
    }

    public override void _Ready()
    {
        base._Ready();
        BuildBody();
        Refresh();
    }

    private void BuildBody()
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 32);
        margin.AddThemeConstantOverride("margin_right", 32);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        margin.AddChild(col);

        // Name input
        _nameEdit = new LineEdit { Text = _template.Name, PlaceholderText = "Template name" };
        _nameEdit.AddThemeFontOverride("font", UIFonts.Title);
        _nameEdit.AddThemeFontSizeOverride("font_size", UIFonts.TitleSize);
        _nameEdit.AddThemeColorOverride("font_color", UIColors.TextBright);
        _nameEdit.CustomMinimumSize = new Vector2(0, 36);
        var editStyle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.4f), BorderColor = UIColors.BorderMid };
        editStyle.SetBorderWidthAll(1);
        editStyle.ContentMarginLeft = 10;
        editStyle.ContentMarginRight = 10;
        _nameEdit.AddThemeStyleboxOverride("normal", editStyle);
        _nameEdit.AddThemeStyleboxOverride("focus", editStyle);
        _nameEdit.TextChanged += t => _template.Name = t;
        col.AddChild(_nameEdit);

        col.AddChild(BuildSeparator());

        // Composition
        var compHeader = new Label { Text = "COMPOSITION" };
        UIFonts.StyleRole(compHeader, UIFonts.Role.Small);
        col.AddChild(compHeader);

        _compositionList = new VBoxContainer();
        _compositionList.AddThemeConstantOverride("separation", 4);
        col.AddChild(_compositionList);

        var addBtn = new Button { Text = "+ ADD SHIP TYPE" };
        addBtn.CustomMinimumSize = new Vector2(0, 32);
        UIFonts.StyleButtonRole(addBtn, UIFonts.Role.Small, UIColors.Accent);
        GlassPanel.StyleButton(addBtn);
        addBtn.Pressed += AddShipTypePrompt;
        col.AddChild(addBtn);

        col.AddChild(BuildSeparator());

        // Role defaults
        var roleHeader = new Label { Text = "FLEET ROLE DEFAULTS" };
        UIFonts.StyleRole(roleHeader, UIFonts.Role.Small);
        col.AddChild(roleHeader);

        _roleDefaultsList = new VBoxContainer();
        _roleDefaultsList.AddThemeConstantOverride("separation", 4);
        col.AddChild(_roleDefaultsList);

        col.AddChild(BuildSeparator());

        // Supply strip (combined)
        var supplyHeader = new Label { Text = "COMBINED SUPPLY PROFILE" };
        UIFonts.StyleRole(supplyHeader, UIFonts.Role.Small);
        col.AddChild(supplyHeader);
        _supplyStrip = new HBoxContainer();
        _supplyStrip.CustomMinimumSize = new Vector2(0, 20);
        col.AddChild(_supplyStrip);

        col.AddChild(BuildSeparator());

        _footerLabel = new Label { Text = "APPLIES TO: \u2014" };
        UIFonts.StyleRole(_footerLabel, UIFonts.Role.Small);
        col.AddChild(_footerLabel);

        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 8);
        col.AddChild(btnRow);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        btnRow.AddChild(spacer);

        var cancel = new Button { Text = "CANCEL" };
        cancel.CustomMinimumSize = new Vector2(120, 36);
        UIFonts.StyleButtonRole(cancel, UIFonts.Role.Small);
        GlassPanel.StyleButton(cancel);
        cancel.Pressed += RequestClose;
        btnRow.AddChild(cancel);

        var save = new Button { Text = "SAVE TEMPLATE" };
        save.CustomMinimumSize = new Vector2(160, 36);
        UIFonts.StyleButtonRole(save, UIFonts.Role.Small, UIColors.TextBright);
        GlassPanel.StyleButton(save, primary: true);
        save.Pressed += OnSave;
        btnRow.AddChild(save);

        Body.AddChild(margin);
    }

    private void AddShipTypePrompt()
    {
        var player = _mainScene?.PlayerEmpire;
        if (player == null || player.DesignState.Designs.Count == 0)
        {
            McpLog.Info("[FleetTemplate] No designs available — author one first");
            return;
        }

        // Simple: add the first design at count=1. Full picker is phase tail work.
        var design = player.DesignState.Designs[0];
        _template.Entries.Add(new FleetTemplateEntry
        {
            DesignId = design.Id,
            Count = 1,
        });
        Refresh();
    }

    public void Refresh()
    {
        foreach (var child in _compositionList.GetChildren()) child.QueueFree();
        foreach (var child in _roleDefaultsList.GetChildren()) child.QueueFree();
        foreach (var child in _supplyStrip.GetChildren()) child.QueueFree();

        var player = _mainScene?.PlayerEmpire;
        if (player == null) return;

        if (_template.Entries.Count == 0)
        {
            var empty = new Label { Text = "No ship types yet. Click + ADD SHIP TYPE." };
            UIFonts.StyleRole(empty, UIFonts.Role.Small);
            _compositionList.AddChild(empty);
        }
        else
        {
            for (int i = 0; i < _template.Entries.Count; i++)
                _compositionList.AddChild(BuildCompositionRow(i, _template.Entries[i]));
        }

        // Role defaults — one row per role used in the composition.
        var rolesUsed = _template.Entries
            .Select(e => FindDesign(e.DesignId)?.GetChassis() != null ? FleetRole.Brawler : FleetRole.NonCombatant)
            .Distinct()
            .ToList();

        foreach (var role in rolesUsed)
        {
            _roleDefaultsList.AddChild(BuildRoleDropdown(role));
        }
        if (rolesUsed.Count == 0)
        {
            var hint = new Label { Text = "Add ship types first." };
            UIFonts.StyleRole(hint, UIFonts.Role.Small);
            _roleDefaultsList.AddChild(hint);
        }

        BuildSupplyStrip();

        int matchingFleets = _mainScene?.Fleets
            .Count(f => _mainScene?.PlayerEmpire?.Id == f.OwnerEmpireId) ?? 0;
        _footerLabel.Text = $"APPLIES TO: {matchingFleets} fleet(s) currently owned";
    }

    private Control BuildCompositionRow(int index, FleetTemplateEntry entry)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        var design = FindDesign(entry.DesignId);

        var count = new Label { Text = $"{entry.Count}\u00D7" };
        UIFonts.StyleRole(count, UIFonts.Role.Normal, UIColors.TextBright);
        count.CustomMinimumSize = new Vector2(40, 0);
        row.AddChild(count);

        var name = new Label { Text = design?.Name.ToUpperInvariant() ?? "(missing)" };
        UIFonts.StyleRole(name, UIFonts.Role.Normal);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(name);

        var plus = new Button { Text = "+" };
        plus.CustomMinimumSize = new Vector2(32, 28);
        UIFonts.StyleButtonRole(plus, UIFonts.Role.Small);
        GlassPanel.StyleButton(plus);
        plus.Pressed += () => { entry.Count++; Refresh(); };
        row.AddChild(plus);

        var minus = new Button { Text = "\u2212" };
        minus.CustomMinimumSize = new Vector2(32, 28);
        UIFonts.StyleButtonRole(minus, UIFonts.Role.Small);
        GlassPanel.StyleButton(minus);
        minus.Pressed += () =>
        {
            entry.Count--;
            if (entry.Count <= 0) _template.Entries.RemoveAt(index);
            Refresh();
        };
        row.AddChild(minus);

        return row;
    }

    private Control BuildRoleDropdown(FleetRole role)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        var label = new Label { Text = role.ToString().ToUpperInvariant() };
        UIFonts.StyleRole(label, UIFonts.Role.Small);
        label.CustomMinimumSize = new Vector2(100, 0);
        row.AddChild(label);

        var dd = new OptionButton();
        foreach (var d in new[] { Disposition.Charge, Disposition.Hold, Disposition.StandBack, Disposition.Retreat })
            dd.AddItem(d.ToString().ToUpperInvariant(), (int)d);
        dd.Select((int)_template.RoleDefaults.GetValueOrDefault(role, Disposition.Hold));
        dd.ItemSelected += idx => _template.RoleDefaults[role] = (Disposition)(int)idx;
        UIFonts.StyleButtonRole(dd, UIFonts.Role.Small);
        GlassPanel.StyleButton(dd);
        dd.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(dd);

        return row;
    }

    private void BuildSupplyStrip()
    {
        // Aggregate supply from each entry's design × count.
        var drains = new Dictionary<PrecursorColor, float>();
        foreach (var entry in _template.Entries)
        {
            var design = FindDesign(entry.DesignId);
            if (design == null) continue;
            var profile = ShipDesignProfiler.Build(design,
                _mainScene?.TechRegistry,
                _mainScene?.PlayerResearchState?.Expertise,
                _mainScene?.PlayerResearchState,
                _mainScene?.PlayerEmpire?.Affinity);
            foreach (var kv in profile.SupplyPerColor)
                drains[kv.Key] = drains.GetValueOrDefault(kv.Key) + kv.Value * entry.Count;
        }

        float total = drains.Values.Sum();
        if (total <= 0f)
        {
            var label = new Label { Text = "NO DRAIN" };
            UIFonts.StyleRole(label, UIFonts.Role.Small);
            _supplyStrip.AddChild(label);
            return;
        }

        foreach (var kv in drains.OrderByDescending(k => k.Value))
        {
            float share = kv.Value / total;
            var seg = new PanelContainer();
            seg.CustomMinimumSize = new Vector2(Mathf.Max(24, 280 * share), 18);
            seg.SizeFlagsStretchRatio = share;
            seg.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            var style = new StyleBoxFlat
            {
                BgColor = new Color(UIColors.GetFactionGlow(kv.Key).R, UIColors.GetFactionGlow(kv.Key).G, UIColors.GetFactionGlow(kv.Key).B, 0.6f),
            };
            style.SetBorderWidthAll(0);
            seg.AddThemeStyleboxOverride("panel", style);

            var lbl = new Label { Text = $"{kv.Value:0.#}" };
            UIFonts.Style(lbl, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBright);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.VerticalAlignment = VerticalAlignment.Center;
            seg.AddChild(lbl);

            _supplyStrip.AddChild(seg);
        }
    }

    private ShipDesign? FindDesign(string id)
    {
        var player = _mainScene?.PlayerEmpire;
        return player?.DesignState.GetDesign(id);
    }

    private void OnSave()
    {
        var player = _mainScene?.PlayerEmpire;
        if (player == null) return;

        _template.Name = _nameEdit.Text;
        if (_isNew) player.DesignState.AddTemplate(_template);

        EventBus.Instance?.FireFleetTemplateSaved(_template.Id);
        McpLog.Info($"[FleetTemplate] Saved '{_template.Name}' ({_template.Id})");
        RequestClose();
    }

    private static FleetTemplate Clone(FleetTemplate src) => new()
    {
        Id = src.Id,
        Name = src.Name,
        Entries = src.Entries.Select(e => new FleetTemplateEntry
        {
            DesignId = e.DesignId,
            Count = e.Count,
            RoleOverride = e.RoleOverride,
        }).ToList(),
        RoleDefaults = new Dictionary<FleetRole, Disposition>(src.RoleDefaults),
    };

    private static Control BuildSeparator()
    {
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(0, 1);
        return sep;
    }
}
