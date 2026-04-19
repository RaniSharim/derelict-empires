using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Tech;
using DerlictEmpires.Nodes.Map;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Content for the RESEARCH tab in LeftPanel. Shows TIER + MOD active blocks and available subsystems.
/// Fonts styled per references/fonts.md — Names use Exo 2 at 13px+, numbers use IBM Plex Mono at 10px+,
/// UI labels use tracked Rajdhani at 10px with TextDim color minimum.
/// </summary>
public partial class ResearchTabContent : VBoxContainer
{
    private MainScene? _mainScene;
    private VBoxContainer _tierBlock = null!;
    private VBoxContainer _modBlock = null!;
    private VBoxContainer _availableList = null!;
    private float _refreshTimer;

    public void Configure(MainScene mainScene)
    {
        _mainScene = mainScene;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _tierBlock = new VBoxContainer { Name = "TierBlock" };
        _tierBlock.AddThemeConstantOverride("separation", 4);
        AddChild(_tierBlock);

        AddChild(new HSeparator());

        _modBlock = new VBoxContainer { Name = "ModBlock" };
        _modBlock.AddThemeConstantOverride("separation", 4);
        AddChild(_modBlock);

        AddChild(new HSeparator());

        var availHeader = new Label { Text = "AVAILABLE SUBSYSTEMS" };
        UIFonts.StyleRole(availHeader, UIFonts.Role.UILabel);
        AddChild(WrapPadded(availHeader, 12, 4));

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        AddChild(scroll);

        _availableList = new VBoxContainer();
        _availableList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _availableList.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_availableList);

        Refresh();
    }

    public override void _Process(double delta)
    {
        _refreshTimer -= (float)delta;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = 0.5f;
            Refresh();
        }
    }

    private void Refresh()
    {
        BuildTierBlock();
        BuildModBlock();
        BuildAvailableList();
    }

    private void BuildTierBlock()
    {
        foreach (var child in _tierBlock.GetChildren()) child.QueueFree();

        var state = _mainScene?.PlayerResearchState;
        var registry = _mainScene?.TechRegistry;

        var header = new Label { Text = "ACTIVE · TIER" };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel);
        _tierBlock.AddChild(WrapPadded(header, 12, 6));

        if (state == null || registry == null)
        {
            AppendMissingState(_tierBlock);
            return;
        }

        if (string.IsNullOrEmpty(state.CurrentTierProject))
        {
            AppendIdleBlock(_tierBlock, "IDLE — No tier research", openLabel: "SELECT TIER");
            return;
        }

        var node = registry.GetNode(state.CurrentTierProject);
        if (node == null) return;

        var glow = UIColors.GetFactionGlow(node.Color);
        var title = new Label
        {
            Text = $"{node.Color.ToString().ToUpperInvariant()} {FormatCategory(node.Category)} T{node.Tier}"
        };
        UIFonts.StyleRole(title, UIFonts.Role.TitleMedium, glow);
        _tierBlock.AddChild(WrapPadded(title, 12, 0));

        _tierBlock.AddChild(WrapPadded(
            BuildProgressBar(state.CurrentTierProgress, node.ResearchCost, glow), 12, 0));

        var rateLine = new Label { Text = $"{state.CurrentTierProgress:F0} / {node.ResearchCost:F0} RP" };
        UIFonts.StyleRole(rateLine, UIFonts.Role.DataSmall, UIColors.TextBody);
        _tierBlock.AddChild(WrapPadded(rateLine, 12, 0));

        _tierBlock.AddChild(WrapPadded(BuildChangeButton(), 12, 4));
    }

    private void BuildModBlock()
    {
        foreach (var child in _modBlock.GetChildren()) child.QueueFree();

        var state = _mainScene?.PlayerResearchState;
        var registry = _mainScene?.TechRegistry;

        var header = new Label { Text = "ACTIVE · MODULE" };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel);
        _modBlock.AddChild(WrapPadded(header, 12, 6));

        if (state == null || registry == null)
        {
            AppendMissingState(_modBlock);
            return;
        }

        if (state.CurrentProject == null)
        {
            AppendIdleBlock(_modBlock, "IDLE · +50% → TIER", openLabel: "SELECT MODULE");
            return;
        }

        var sub = registry.GetSubsystem(state.CurrentProject);
        if (sub == null) return;

        var glow = UIColors.GetFactionGlow(sub.Color);
        var title = new Label { Text = sub.DisplayName.ToUpperInvariant() };
        UIFonts.StyleRole(title, UIFonts.Role.TitleMedium, glow);
        _modBlock.AddChild(WrapPadded(title, 12, 0));

        _modBlock.AddChild(WrapPadded(
            BuildProgressBar(state.CurrentProgress, sub.ResearchCost, glow), 12, 0));

        var rateLine = new Label { Text = $"{state.CurrentProgress:F0} / {sub.ResearchCost:F0} RP" };
        UIFonts.StyleRole(rateLine, UIFonts.Role.DataSmall, UIColors.TextBody);
        _modBlock.AddChild(WrapPadded(rateLine, 12, 0));

        _modBlock.AddChild(WrapPadded(BuildChangeButton(), 12, 4));
    }

    private void AppendMissingState(VBoxContainer host)
    {
        var empty = new Label { Text = "No research state yet." };
        UIFonts.StyleRole(empty, UIFonts.Role.BodyPrimary, UIColors.TextDim);
        host.AddChild(WrapPadded(empty, 12, 0));
    }

    private void AppendIdleBlock(VBoxContainer host, string idleText, string openLabel)
    {
        var idle = new Label { Text = idleText };
        UIFonts.StyleRole(idle, UIFonts.Role.BodyPrimary, UIColors.TextDim);
        host.AddChild(WrapPadded(idle, 12, 0));

        var openBtn = new Button { Text = openLabel };
        openBtn.CustomMinimumSize = new Vector2(0, 36);
        UIFonts.StyleButtonRole(openBtn, UIFonts.Role.UILabel, UIColors.Accent);
        GlassPanel.StyleButton(openBtn, primary: true);
        openBtn.Pressed += OpenTechTree;
        host.AddChild(WrapPadded(openBtn, 12, 4));
    }

    private Button BuildChangeButton()
    {
        var changeBtn = new Button { Text = "CHANGE" };
        changeBtn.CustomMinimumSize = new Vector2(0, 32);
        UIFonts.StyleButtonRole(changeBtn, UIFonts.Role.UILabel, UIColors.TextLabel);
        GlassPanel.StyleButton(changeBtn);
        changeBtn.Pressed += OpenTechTree;
        return changeBtn;
    }

    private void BuildAvailableList()
    {
        foreach (var child in _availableList.GetChildren()) child.QueueFree();

        var state = _mainScene?.PlayerResearchState;
        var registry = _mainScene?.TechRegistry;
        if (state == null || registry == null) return;

        var items = state.AvailableSubsystems
            .Where(id => id != state.CurrentProject)
            .Select(id => registry.GetSubsystem(id))
            .Where(s => s != null)
            .OrderBy(s => s!.Color)
            .ThenBy(s => s!.Category)
            .ThenBy(s => s!.Tier)
            .ToList();

        if (items.Count == 0)
        {
            var empty = new Label { Text = "No subsystems available.\nUnlock a tier first via Tech Tree." };
            UIFonts.StyleRole(empty, UIFonts.Role.BodyPrimary, UIColors.TextDim);
            empty.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _availableList.AddChild(WrapPadded(empty, 12, 8));
            return;
        }

        foreach (var sub in items)
            _availableList.AddChild(BuildSubsystemRow(sub!, state, registry));
    }

    private Control BuildSubsystemRow(SubsystemData sub, EmpireResearchState state, TechTreeRegistry registry)
    {
        var glow = UIColors.GetFactionGlow(sub.Color);

        var btn = new Button { Text = $"  {sub.DisplayName}  (T{sub.Tier})" };
        btn.Alignment = HorizontalAlignment.Left;
        btn.CustomMinimumSize = new Vector2(0, 32);
        btn.ClipText = true;
        btn.FocusMode = Control.FocusModeEnum.None;
        // Title Medium for the name — Exo 2 at its 13px floor, faction-glow color.
        UIFonts.StyleButtonRole(btn, UIFonts.Role.TitleMedium, glow);
        GlassPanel.StyleButton(btn);
        btn.Pressed += () =>
        {
            EventBus.Instance?.FireTechTreeOpenRequested(new TechTreeOpenRequest
            {
                Color = sub.Color,
                Category = sub.Category,
                Tier = sub.Tier,
                SubsystemId = sub.Id,
                Intent = TechTreeIntent.View,
            });
        };

        return WrapPadded(btn, 12, 0);
    }

    private static Control BuildProgressBar(float current, float total, Color glow)
    {
        var host = new Control();
        host.CustomMinimumSize = new Vector2(0, 4);

        var track = new ColorRect
        {
            Color = new Color(20 / 255f, 30 / 255f, 48 / 255f, 0.9f),
        };
        track.SetAnchorsPreset(LayoutPreset.FullRect);
        host.AddChild(track);

        float ratio = total > 0 ? Mathf.Clamp(current / total, 0f, 1f) : 0f;
        var fill = new ColorRect
        {
            Color = new Color(glow.R, glow.G, glow.B, 0.85f),
        };
        fill.SetAnchorsPreset(LayoutPreset.LeftWide);
        fill.AnchorRight = ratio;
        host.AddChild(fill);

        return host;
    }

    private static Control WrapPadded(Control inner, int horizontal, int vertical)
    {
        var m = new MarginContainer();
        m.AddThemeConstantOverride("margin_left", horizontal);
        m.AddThemeConstantOverride("margin_right", horizontal);
        m.AddThemeConstantOverride("margin_top", vertical);
        m.AddThemeConstantOverride("margin_bottom", vertical);
        m.AddChild(inner);
        return m;
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

    private static void OpenTechTree()
    {
        var empire = GameManager.Instance?.LocalPlayerEmpire;
        var color = empire?.Affinity ?? PrecursorColor.Red;
        EventBus.Instance?.FireTechTreeOpenRequested(new TechTreeOpenRequest
        {
            Color = color,
            Intent = TechTreeIntent.View,
        });
    }
}
