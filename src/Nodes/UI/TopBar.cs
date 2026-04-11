using Godot;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Top HUD bar: logo | credits | 5 faction resource boxes | layer toggles.
/// Height 80px, full width. Uses Control (not PanelContainer) to enforce fixed height.
/// </summary>
public partial class TopBar : Control
{
    public const int BarHeight = 80;

    private Label _creditAmount = null!;
    private Label _creditDelta = null!;
    private readonly Dictionary<PrecursorColor, FactionResourceBox> _factionBoxes = new();

    // Layer toggle state — exposed so map renderers can read it
    public bool ShowFleets { get; private set; } = true;
    public bool ShowNebulae { get; private set; }
    public bool ShowSystems { get; private set; } = true;

    public override void _Ready()
    {
        // Fixed 80px tall, full width, top of screen
        AnchorLeft = 0;
        AnchorRight = 1;
        AnchorTop = 0;
        AnchorBottom = 0;
        OffsetTop = 0;
        OffsetBottom = BarHeight;
        OffsetLeft = 0;
        OffsetRight = 0;
        ClipContents = true;
        ZIndex = 100;

        // Background panel (styled using GlassPanel logic)
        var bg = new PanelContainer { Name = "Background" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        GlassPanel.Apply(bg, enableBlur: true);
        AddChild(bg);

        // Main HBox fills the 80px height
        var main = new HBoxContainer { Name = "TopBarLayout" };
        main.SetAnchorsPreset(LayoutPreset.FullRect);
        main.AddThemeConstantOverride("separation", 0);
        AddChild(main);

        // Section 1 — Logo
        BuildLogoSection(main);
        AddVerticalDivider(main);

        // Section 2 — Credits
        BuildCreditsSection(main);
        AddVerticalDivider(main);

        // Section 3 — Faction resource boxes
        BuildFactionBoxes(main);
        AddVerticalDivider(main);

        // Section 4 — Layer toggles
        BuildLayerToggles(main);

        // Bottom accent line
        var accent = new ColorRect { Name = "BottomAccent" };
        accent.AnchorLeft = 0;
        accent.AnchorRight = 1;
        accent.AnchorTop = 1;
        accent.AnchorBottom = 1;
        accent.OffsetTop = -1;
        accent.OffsetBottom = 0;
        accent.Color = new Color(UIColors.Accent, 0.70f);
        accent.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(accent);
    }

    private void BuildLogoSection(HBoxContainer parent)
    {
        var logo = new Control();
        logo.CustomMinimumSize = new Vector2(180, 0);
        logo.SizeFlagsVertical = SizeFlags.ExpandFill;
        parent.AddChild(logo);

        var logoVBox = new VBoxContainer();
        logoVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        logoVBox.OffsetLeft = 16;
        logoVBox.Alignment = BoxContainer.AlignmentMode.Center;
        logoVBox.AddThemeConstantOverride("separation", -2);
        logo.AddChild(logoVBox);

        var title = new Label { Text = "DERELICT EMPIRES" };
        UIFonts.Style(title, UIFonts.Exo2SemiBold, 16, UIColors.TextBright);
        logoVBox.AddChild(title);

        // Subtitle showing empire info
        var subtitle = new Label { Text = "Exo 2, Triple, Blue" };
        UIFonts.Style(subtitle, UIFonts.ShareTechMono, 8, UIColors.TextFaint);
        logoVBox.AddChild(subtitle);

        // Cyan underline accent
        var underline = new ColorRect();
        underline.CustomMinimumSize = new Vector2(120, 2);
        underline.Color = new Color(UIColors.Accent, 0.6f);
        logoVBox.AddChild(underline);
    }

    private void BuildCreditsSection(HBoxContainer parent)
    {
        var credits = new PanelContainer();
        credits.CustomMinimumSize = new Vector2(150, 0);
        credits.SizeFlagsVertical = SizeFlags.ExpandFill;
        var creditsStyle = new StyleBoxFlat();
        creditsStyle.BgColor = UIColors.CreditBg;
        creditsStyle.ContentMarginLeft = 12;
        creditsStyle.ContentMarginRight = 12;
        creditsStyle.SetBorderWidthAll(0);
        creditsStyle.SetCornerRadiusAll(0);
        credits.AddThemeStyleboxOverride("panel", creditsStyle);
        parent.AddChild(credits);

        var creditsHBox = new HBoxContainer();
        creditsHBox.Alignment = BoxContainer.AlignmentMode.Center;
        creditsHBox.AddThemeConstantOverride("separation", 8);
        credits.AddChild(creditsHBox);

        // Green circle icon
        var icon = new CreditIcon();
        icon.CustomMinimumSize = new Vector2(28, 28);
        icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        creditsHBox.AddChild(icon);

        var creditsVBox = new VBoxContainer();
        creditsVBox.Alignment = BoxContainer.AlignmentMode.Center;
        creditsVBox.AddThemeConstantOverride("separation", 0);
        creditsHBox.AddChild(creditsVBox);

        _creditAmount = new Label { Text = "0" };
        UIFonts.Style(_creditAmount, UIFonts.ShareTechMono, 14, UIColors.CreditText);
        creditsVBox.AddChild(_creditAmount);

        _creditDelta = new Label { Text = "+0" };
        UIFonts.Style(_creditDelta, UIFonts.ShareTechMono, 10, UIColors.DeltaPos);
        creditsVBox.AddChild(_creditDelta);
    }

    private void BuildFactionBoxes(HBoxContainer parent)
    {
        var container = new HBoxContainer();
        container.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        container.SizeFlagsVertical = SizeFlags.ExpandFill;
        container.AddThemeConstantOverride("separation", 0);
        parent.AddChild(container);

        var factions = new[] { PrecursorColor.Red, PrecursorColor.Blue,
                               PrecursorColor.Green, PrecursorColor.Gold,
                               PrecursorColor.Purple };

        for (int i = 0; i < factions.Length; i++)
        {
            var color = factions[i];
            var box = new FactionResourceBox(color) { Name = $"Faction_{color}" };
            container.AddChild(box);
            _factionBoxes[color] = box;

            if (i < factions.Length - 1)
                AddVerticalDivider(container);
        }
    }

    private void BuildLayerToggles(HBoxContainer parent)
    {
        var section = new Control();
        section.CustomMinimumSize = new Vector2(130, 0);
        section.SizeFlagsVertical = SizeFlags.ExpandFill;
        parent.AddChild(section);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.OffsetLeft = 10;
        vbox.OffsetRight = -10;
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 4);
        section.AddChild(vbox);

        AddToggleRow(vbox, "FLEETS", ShowFleets, on => ShowFleets = on);
        AddToggleRow(vbox, "NEBULAE", ShowNebulae, on => ShowNebulae = on);
        AddToggleRow(vbox, "SYSTEMS", ShowSystems, on => ShowSystems = on);
    }

    private static void AddToggleRow(VBoxContainer parent, string label, bool initialState, System.Action<bool> onToggle)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        parent.AddChild(row);

        var lbl = new Label { Text = label };
        UIFonts.Style(lbl, UIFonts.BarlowSemiBold, 9, UIColors.TextLabel);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lbl.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(lbl);

        var pill = new Button();
        pill.CustomMinimumSize = new Vector2(36, 18);
        pill.ToggleMode = true;
        pill.ButtonPressed = initialState;
        pill.Text = initialState ? "ON" : "OFF";
        UIFonts.StyleButton(pill, UIFonts.ShareTechMono, 8,
            initialState ? new Color("#0a0e14") : UIColors.TextDim);
        StyleTogglePill(pill, initialState);

        pill.Toggled += pressed =>
        {
            pill.Text = pressed ? "ON" : "OFF";
            UIFonts.StyleButton(pill, UIFonts.ShareTechMono, 8,
                pressed ? new Color("#0a0e14") : UIColors.TextDim);
            StyleTogglePill(pill, pressed);
            onToggle(pressed);
        };
        row.AddChild(pill);
    }

    private static void StyleTogglePill(Button pill, bool on)
    {
        var style = new StyleBoxFlat();
        style.SetCornerRadiusAll(9);

        if (on)
        {
            style.BgColor = UIColors.GreenGlow;
            pill.AddThemeColorOverride("font_color", new Color("#0a0e14"));
        }
        else
        {
            style.BgColor = new Color(60 / 255f, 70 / 255f, 80 / 255f, 0.6f);
            pill.AddThemeColorOverride("font_color", UIColors.TextDim);
        }
        style.SetBorderWidthAll(0);
        pill.AddThemeStyleboxOverride("normal", style);
        pill.AddThemeStyleboxOverride("hover", style);
        pill.AddThemeStyleboxOverride("pressed", style);
        pill.AddThemeStyleboxOverride("focus", style);
    }

    private static void AddVerticalDivider(HBoxContainer parent)
    {
        var div = new ColorRect();
        div.CustomMinimumSize = new Vector2(1, 0);
        div.SizeFlagsVertical = SizeFlags.ExpandFill;
        div.Color = UIColors.BorderBright;
        parent.AddChild(div);
    }

    public override void _Process(double delta)
    {
        var empire = GameManager.Instance?.LocalPlayerEmpire;
        if (empire == null) return;

        long credits = empire.Credits;
        _creditAmount.Text = FormatCredits(credits);
    }

    /// <summary>Push income data to all faction boxes.</summary>
    public void UpdateIncome(Dictionary<string, float> income)
    {
        foreach (var box in _factionBoxes.Values)
            box.UpdateIncome(income);
    }

    private static string FormatCredits(long amount)
    {
        if (amount >= 1_000_000) return $"{amount / 1_000_000.0:F2}M";
        if (amount >= 1_000) return $"{amount:N0}";
        return amount.ToString();
    }
}

/// <summary>Green circle icon for the credits display.</summary>
public partial class CreditIcon : Control
{
    public override void _Draw()
    {
        float r = Mathf.Min(Size.X, Size.Y) / 2f;
        var center = Size / 2f;
        DrawCircle(center, r, UIColors.GreenGlow);
        // Inner darker circle for depth
        DrawCircle(center, r * 0.7f, new Color(UIColors.GreenGlow, 0.4f));
    }
}
