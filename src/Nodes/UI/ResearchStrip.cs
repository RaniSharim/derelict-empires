using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Services;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Always-visible topbar research strip showing both the TIER and MOD active projects.
/// Per research_ui_spec.md §3 (two-track model). Click opens the Tech Tree overlay.
/// </summary>
public partial class ResearchStrip : Control
{
    public const int StripWidth = 260;
    public const int StripHeight = 68;

    private ResearchTrackRow _tierRow = null!;
    private ResearchTrackRow _modRow = null!;

    private IGameQuery? _query;

    public void Configure(IGameQuery query) => _query = query;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(StripWidth, StripHeight);
        MouseFilter = MouseFilterEnum.Pass;

        var bg = new PanelContainer { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        var style = new StyleBoxFlat
        {
            BgColor = new Color(10 / 255f, 14 / 255f, 24 / 255f, 0.6f),
        };
        style.SetBorderWidthAll(0);
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        style.SetCornerRadiusAll(0);
        bg.AddThemeStyleboxOverride("panel", style);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 3);
        bg.AddChild(vbox);

        _tierRow = new ResearchTrackRow();
        _tierRow.SetLabel("TIER");
        vbox.AddChild(_tierRow);

        _modRow = new ResearchTrackRow();
        _modRow.SetLabel("MOD");
        vbox.AddChild(_modRow);

        GuiInput += OnGuiInput;
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            var empire = GameManager.Instance?.LocalPlayerEmpire;
            var primaryColor = empire?.Affinity ?? PrecursorColor.Red;
            EventBus.Instance?.FireTechTreeOpenRequested(new TechTreeOpenRequest
            {
                Color = primaryColor,
                Intent = TechTreeIntent.View,
            });
            AcceptEvent();
        }
    }

    public override void _Process(double delta)
    {
        var state = _query?.PlayerResearchState;
        var registry = _query?.TechRegistry;

        if (state == null || registry == null)
        {
            _tierRow.ShowIdle("NO STATE");
            _modRow.ShowIdle("NO STATE");
            return;
        }

        UpdateTierRow(state, registry);
        UpdateModRow(state, registry);
    }

    private void UpdateTierRow(EmpireResearchState state, TechTreeRegistry registry)
    {
        if (state.CurrentTierProject == null)
        {
            _tierRow.ShowIdle("IDLE — SELECT");
            return;
        }

        var node = registry.GetNode(state.CurrentTierProject);
        if (node == null) { _tierRow.ShowIdle(state.CurrentTierProject); return; }

        var glow = UIColors.GetFactionGlow(node.Color);
        string label = $"{node.Color.ToString().ToUpperInvariant()} {FormatCategory(node.Category)} T{node.Tier}";
        float ratio = node.ResearchCost > 0 ? state.CurrentTierProgress / node.ResearchCost : 0f;
        _tierRow.ShowActive(label, glow, ratio);
    }

    private void UpdateModRow(EmpireResearchState state, TechTreeRegistry registry)
    {
        if (state.CurrentProject == null)
        {
            _modRow.ShowIdle("IDLE · +50% → TIER");
            return;
        }

        var sub = registry.GetSubsystem(state.CurrentProject);
        if (sub == null) { _modRow.ShowIdle(state.CurrentProject); return; }

        var glow = UIColors.GetFactionGlow(sub.Color);
        float ratio = sub.ResearchCost > 0 ? state.CurrentProgress / sub.ResearchCost : 0f;
        _modRow.ShowActive(sub.DisplayName.ToUpperInvariant(), glow, ratio);
    }

    private static string FormatCategory(TechCategory cat) => cat switch
    {
        TechCategory.WeaponsEnergyPropulsion => "WEAPONS",
        TechCategory.ComputingSensors => "SENSORS",
        TechCategory.IndustryMining => "INDUSTRY",
        TechCategory.AdminLogistics => "LOGISTICS",
        TechCategory.Special => "SPECIAL",
        _ => cat.ToString().ToUpperInvariant()
    };
}

/// <summary>One row of the research strip: label, progress bar, project name, percentage.</summary>
public partial class ResearchTrackRow : HBoxContainer
{
    private Label _labelCaption = null!;
    private Label _projectName = null!;
    private Label _progressValue = null!;
    private ColorRect _barFill = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 24);
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 8);

        _labelCaption = new Label();
        // UI Label role — mono, 10px tracked ALL-CAPS, TextDim (readable).
        UIFonts.StyleRole(_labelCaption, UIFonts.Role.StatusBadge);
        _labelCaption.CustomMinimumSize = new Vector2(32, 0);
        _labelCaption.VerticalAlignment = VerticalAlignment.Center;
        AddChild(_labelCaption);

        // Progress bar box
        var barBox = new Control();
        barBox.CustomMinimumSize = new Vector2(78, 0);
        barBox.SizeFlagsVertical = SizeFlags.Fill;
        AddChild(barBox);

        var barTrack = new ColorRect { Color = new Color(20 / 255f, 30 / 255f, 48 / 255f, 0.9f) };
        barTrack.SetAnchorsPreset(LayoutPreset.LeftWide);
        barTrack.AnchorRight = 1;
        barTrack.OffsetTop = 8;
        barTrack.OffsetBottom = 11;
        barTrack.MouseFilter = MouseFilterEnum.Ignore;
        barBox.AddChild(barTrack);

        _barFill = new ColorRect { Color = UIColors.Accent };
        _barFill.SetAnchorsPreset(LayoutPreset.LeftWide);
        _barFill.AnchorRight = 0;
        _barFill.OffsetTop = 8;
        _barFill.OffsetBottom = 11;
        _barFill.MouseFilter = MouseFilterEnum.Ignore;
        barBox.AddChild(_barFill);

        _projectName = new Label { Text = "" };
        // Title Medium role — Exo 2 SemiBold at 13px, bright. Names need the floor.
        UIFonts.StyleRole(_projectName, UIFonts.Role.TitleMedium, UIColors.TextDim);
        _projectName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _projectName.VerticalAlignment = VerticalAlignment.Center;
        _projectName.ClipText = true;
        AddChild(_projectName);

        _progressValue = new Label { Text = "" };
        // Data Small role — mono 10px, readable as a number.
        UIFonts.StyleRole(_progressValue, UIFonts.Role.DataSmall);
        _progressValue.VerticalAlignment = VerticalAlignment.Center;
        _progressValue.CustomMinimumSize = new Vector2(38, 0);
        _progressValue.HorizontalAlignment = HorizontalAlignment.Right;
        AddChild(_progressValue);

        if (_pendingLabel != null)
        {
            _labelCaption.Text = _pendingLabel;
            _pendingLabel = null;
        }
    }

    public void SetLabel(string caption)
    {
        if (_labelCaption != null) _labelCaption.Text = caption;
        else _pendingLabel = caption;
    }

    public void ShowIdle(string message)
    {
        if (_projectName == null || _progressValue == null || _barFill == null) return;
        _projectName.Text = message;
        _projectName.AddThemeColorOverride("font_color", UIColors.TextDim);
        _progressValue.Text = "";
        _barFill.AnchorRight = 0;
    }

    public void ShowActive(string projectLabel, Color glow, float ratio)
    {
        if (_projectName == null || _progressValue == null || _barFill == null) return;
        _projectName.Text = projectLabel;
        _projectName.AddThemeColorOverride("font_color", glow);
        _progressValue.Text = $"{Mathf.Clamp(ratio, 0f, 1f) * 100f:F0}%";
        _progressValue.AddThemeColorOverride("font_color", glow);
        _barFill.AnchorRight = Mathf.Clamp(ratio, 0f, 1f);
        _barFill.Color = new Color(glow.R, glow.G, glow.B, 0.85f);
    }

    private string? _pendingLabel;
}
