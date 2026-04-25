using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Services;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Full-screen Ship Designer overlay — three-pane layout
/// (ChassisPane · SlotMatrix · ProfilePane) + footer action strip.
/// Transient draft state lives here; persistence goes through EmpireDesignState
/// on the owning EmpireData via the local player's state.
/// </summary>
public partial class ShipDesignerOverlay : GlassOverlay
{
    public enum Mode { NewDraft, EditExisting }

    private IGameQuery? _query;

    /// <summary>The live draft being edited. Swapped on chassis change (slot count migration).</summary>
    public ShipDesign Draft { get; private set; } = new();

    /// <summary>The saved copy at open-time (for delta-vs-saved in ProfilePane). Null for new drafts.</summary>
    public ShipDesign? OriginalSnapshot { get; private set; }

    /// <summary>True when the draft differs from the saved copy (or is new).</summary>
    public bool IsDirty { get; private set; }

    public Mode CurrentMode { get; private set; } = Mode.NewDraft;

    private ChassisPane _chassisPane = null!;
    private SlotMatrix _slotMatrix = null!;
    private ProfilePane _profilePane = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public ShipDesignerOverlay()
    {
        OverlayTitle = "SHIP DESIGNER";
    }

    public void Configure(IGameQuery query, DesignerOpenRequest request)
    {
        _query = query;

        var player = query.PlayerEmpire;
        if (player != null && !string.IsNullOrEmpty(request.DesignId))
        {
            var existing = player.DesignState.GetDesign(request.DesignId!);
            if (existing != null)
            {
                OriginalSnapshot = CloneDesign(existing);
                Draft = CloneDesign(existing);
                CurrentMode = Mode.EditExisting;
                return;
            }
        }

        // New draft — pick requested chassis or default to a mid-weight cruiser
        string chassisId = request.ChassisId
            ?? ChassisData.All.FirstOrDefault(c => c.Id == "cruiser_weapons")?.Id
            ?? ChassisData.All[0].Id;

        Draft = new ShipDesign
        {
            Name = "New Design",
            ChassisId = chassisId,
            SlotFills = new List<string>(),
        };
        EnsureSlotFillLength();
        CurrentMode = Mode.NewDraft;
    }

    public override void _Ready()
    {
        base._Ready();
        BuildLayout();
        RefreshAll();
    }

    // === Public API used by child panes =====================================

    public IGameQuery? Query => _query;

    public TechTreeRegistry? Registry => _query?.TechRegistry;

    public EmpireResearchState? ResearchState => _query?.PlayerResearchState;

    public ExpertiseTracker? Expertise => ResearchState?.Expertise;

    public PrecursorColor? EmpireAffinity => _query?.PlayerEmpire?.Affinity;

    /// <summary>Swap the chassis. Migrates SlotFills — extra entries moved to orphan tray; shortfall padded.</summary>
    public void SetChassis(string chassisId)
    {
        if (Draft.ChassisId == chassisId) return;

        Draft.ChassisId = chassisId;
        EnsureSlotFillLength();
        MarkDirty();
        RefreshAll();
    }

    /// <summary>Fill (or clear, if subsystemId is null/empty) a single slot index.</summary>
    public void SetSlot(int index, string? subsystemId)
    {
        EnsureSlotFillLength();
        if (index < 0 || index >= Draft.SlotFills.Count) return;
        Draft.SlotFills[index] = subsystemId ?? "";
        MarkDirty();
        RefreshAll();
    }

    public void SetDesignName(string name)
    {
        Draft.Name = name;
        MarkDirty();
    }

    /// <summary>True when SAVE should be enabled: no validator errors.</summary>
    public bool CanSave()
    {
        var researched = ResearchState?.ResearchedSubsystems;
        var result = ShipDesignValidator.Validate(Draft, researched);
        return result.IsValid;
    }

    public ShipDesignProfiler.Profile CurrentProfile() =>
        ShipDesignProfiler.Build(Draft, Registry, Expertise, ResearchState, EmpireAffinity);

    public void RefreshAll()
    {
        _chassisPane?.Refresh();
        _slotMatrix?.Refresh();
        _profilePane?.Refresh();
        UpdateSaveButton();
    }

    // === Persistence ========================================================

    private void OnSavePressed()
    {
        if (!CanSave()) return;
        var player = _query?.PlayerEmpire;
        if (player == null) return;

        string designId;
        if (CurrentMode == Mode.EditExisting && !string.IsNullOrEmpty(Draft.Id))
        {
            // Overwrite in place
            var existing = player.DesignState.GetDesign(Draft.Id);
            if (existing != null)
            {
                existing.Name = Draft.Name;
                existing.ChassisId = Draft.ChassisId;
                existing.SlotFills = new List<string>(Draft.SlotFills);
                existing.Extras = new List<string>(Draft.Extras);
            }
            designId = Draft.Id;
        }
        else
        {
            player.DesignState.AddDesign(Draft);
            designId = Draft.Id;
            CurrentMode = Mode.EditExisting;
        }

        OriginalSnapshot = CloneDesign(Draft);
        IsDirty = false;
        EventBus.Instance?.FireDesignSaved(designId);
        McpLog.Info($"[Designer] Saved design '{Draft.Name}' ({designId})");
        RequestClose();
    }

    public override void RequestClose()
    {
        if (!IsDirty)
        {
            base.RequestClose();
            return;
        }
        // Dirty — currently a simple discard path; full 3-option prompt is Phase F.
        McpLog.Info("[Designer] Discarding unsaved draft");
        base.RequestClose();
    }

    // === Layout =============================================================

    private void BuildLayout()
    {
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride("separation", 12);

        _chassisPane = new ChassisPane(this) { Name = "ChassisPane" };
        _chassisPane.CustomMinimumSize = new Vector2(260, 0);
        _chassisPane.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        row.AddChild(_chassisPane);

        _slotMatrix = new SlotMatrix(this) { Name = "SlotMatrix" };
        _slotMatrix.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _slotMatrix.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        row.AddChild(_slotMatrix);

        _profilePane = new ProfilePane(this) { Name = "ProfilePane" };
        _profilePane.CustomMinimumSize = new Vector2(280, 0);
        _profilePane.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        row.AddChild(_profilePane);

        Body.AddChild(WrapWithMargin(row));
        Body.AddChild(BuildFooter());
    }

    private Control WrapWithMargin(Control inner)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 0);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        margin.AddChild(inner);
        return margin;
    }

    private Control BuildFooter()
    {
        var footer = new PanelContainer { Name = "Footer" };
        footer.CustomMinimumSize = new Vector2(0, 56);

        var bg = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.15f) };
        bg.SetBorderWidthAll(0);
        bg.BorderWidthTop = 1;
        bg.BorderColor = UIColors.BorderDim;
        footer.AddThemeStyleboxOverride("panel", bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        footer.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        margin.AddChild(row);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(spacer);

        _cancelButton = new Button { Text = "CANCEL" };
        _cancelButton.CustomMinimumSize = new Vector2(120, 36);
        UIFonts.StyleButtonRole(_cancelButton, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(_cancelButton);
        _cancelButton.Pressed += RequestClose;
        row.AddChild(_cancelButton);

        _saveButton = new Button { Text = "SAVE DESIGN" };
        _saveButton.CustomMinimumSize = new Vector2(160, 36);
        UIFonts.StyleButtonRole(_saveButton, UIFonts.Role.UILabel, UIColors.TextBright);
        StylePrimaryButton(_saveButton);
        _saveButton.Pressed += OnSavePressed;
        row.AddChild(_saveButton);

        return footer;
    }

    private void UpdateSaveButton()
    {
        if (_saveButton == null) return;
        _saveButton.Disabled = !CanSave();
    }

    private static void StylePrimaryButton(Button btn)
    {
        var normal = new StyleBoxFlat { BgColor = new Color(UIColors.Accent.R, UIColors.Accent.G, UIColors.Accent.B, 0.25f) };
        normal.SetBorderWidthAll(1);
        normal.BorderColor = UIColors.Accent;
        normal.SetCornerRadiusAll(2);

        var hover = new StyleBoxFlat { BgColor = new Color(UIColors.Accent.R, UIColors.Accent.G, UIColors.Accent.B, 0.38f) };
        hover.SetBorderWidthAll(1);
        hover.BorderColor = UIColors.Accent;
        hover.SetCornerRadiusAll(2);

        var disabled = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.25f) };
        disabled.SetBorderWidthAll(1);
        disabled.BorderColor = UIColors.BorderDim;
        disabled.SetCornerRadiusAll(2);

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", hover);
        btn.AddThemeStyleboxOverride("disabled", disabled);
    }

    // === Helpers ============================================================

    private void EnsureSlotFillLength()
    {
        var chassis = Draft.GetChassis();
        if (chassis == null) return;
        while (Draft.SlotFills.Count < chassis.BigSystemSlots) Draft.SlotFills.Add("");
        while (Draft.SlotFills.Count > chassis.BigSystemSlots)
            Draft.SlotFills.RemoveAt(Draft.SlotFills.Count - 1);
    }

    private void MarkDirty()
    {
        IsDirty = true;
        UpdateSaveButton();
    }

    private static ShipDesign CloneDesign(ShipDesign src) => new()
    {
        Id = src.Id,
        Name = src.Name,
        ChassisId = src.ChassisId,
        SlotFills = new List<string>(src.SlotFills),
        Extras = new List<string>(src.Extras),
        ScanStrength = src.ScanStrength,
        ExtractionStrength = src.ExtractionStrength,
        Speed = src.Speed,
    };
}
