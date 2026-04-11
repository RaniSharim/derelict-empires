using Godot;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Top HUD bar: logo | credits | 5 faction resource boxes.
/// Height 68px, full width. Uses Control (not PanelContainer) to enforce fixed height.
/// </summary>
public partial class TopBar : Control
{
    private Label _creditAmount = null!;
    private Label _creditDelta = null!;
    private readonly Dictionary<PrecursorColor, FactionResourceBox> _factionBoxes = new();

    public override void _Ready()
    {
        // Fixed 68px tall, full width, top of screen
        AnchorLeft = 0;
        AnchorRight = 1;
        AnchorTop = 0;
        AnchorBottom = 0;
        OffsetTop = 0;
        OffsetBottom = 68;
        OffsetLeft = 0;
        OffsetRight = 0;
        ClipContents = true;
        ZIndex = 100;

        // Background panel (styled child, not the container itself)
        var bg = new Panel { Name = "Background" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = UIColors.GlassDarkFlat;
        bgStyle.SetBorderWidthAll(0);
        bgStyle.BorderWidthBottom = 1;
        bgStyle.BorderColor = UIColors.BorderBright;
        bgStyle.SetCornerRadiusAll(0);
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        AddChild(bg);

        // Main HBox fills the 68px height
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
        logo.CustomMinimumSize = new Vector2(160, 0);
        logo.SizeFlagsVertical = SizeFlags.ExpandFill;
        parent.AddChild(logo);

        var logoVBox = new VBoxContainer();
        logoVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        logoVBox.Alignment = BoxContainer.AlignmentMode.Center;
        logoVBox.AddThemeConstantOverride("separation", -2);
        logo.AddChild(logoVBox);

        var title1 = new Label { Text = "DERELICT" };
        UIFonts.Style(title1, UIFonts.Exo2SemiBold, 15, UIColors.TextLabel);
        logoVBox.AddChild(title1);

        var title2 = new Label { Text = "EMPIRES" };
        UIFonts.Style(title2, UIFonts.Exo2SemiBold, 15, UIColors.TextLabel);
        logoVBox.AddChild(title2);
    }

    private void BuildCreditsSection(HBoxContainer parent)
    {
        var credits = new PanelContainer();
        credits.CustomMinimumSize = new Vector2(140, 0);
        credits.SizeFlagsVertical = SizeFlags.ExpandFill;
        var creditsStyle = new StyleBoxFlat();
        creditsStyle.BgColor = UIColors.CreditBg;
        creditsStyle.ContentMarginLeft = 12;
        creditsStyle.ContentMarginRight = 12;
        creditsStyle.SetBorderWidthAll(0);
        creditsStyle.SetCornerRadiusAll(0);
        credits.AddThemeStyleboxOverride("panel", creditsStyle);
        parent.AddChild(credits);

        var creditsVBox = new VBoxContainer();
        creditsVBox.Alignment = BoxContainer.AlignmentMode.Center;
        creditsVBox.AddThemeConstantOverride("separation", 0);
        credits.AddChild(creditsVBox);

        _creditAmount = new Label { Text = "0" };
        UIFonts.Style(_creditAmount, UIFonts.ShareTechMono, 14, UIColors.CreditText);
        creditsVBox.AddChild(_creditAmount);

        _creditDelta = new Label { Text = "+0" };
        UIFonts.Style(_creditDelta, UIFonts.ShareTechMono, 10, UIColors.DeltaPos);
        creditsVBox.AddChild(_creditDelta);

        var creditsLabel = new Label { Text = "CREDITS" };
        UIFonts.Style(creditsLabel, UIFonts.BarlowRegular, 7, UIColors.TextDim);
        creditsVBox.AddChild(creditsLabel);
    }

    private void BuildFactionBoxes(HBoxContainer parent)
    {
        var container = new HBoxContainer();
        container.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        container.SizeFlagsVertical = SizeFlags.ExpandFill;
        container.AddThemeConstantOverride("separation", 2);
        parent.AddChild(container);

        foreach (var color in new[] { PrecursorColor.Red, PrecursorColor.Blue,
                                       PrecursorColor.Green, PrecursorColor.Gold,
                                       PrecursorColor.Purple })
        {
            var box = new FactionResourceBox(color) { Name = $"Faction_{color}" };
            container.AddChild(box);
            _factionBoxes[color] = box;
        }
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
