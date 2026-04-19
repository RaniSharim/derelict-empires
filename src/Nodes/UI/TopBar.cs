using Godot;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Top HUD bar: logo | money+food | 5 faction resource boxes.
/// Height 110px, full width. Uses Control (not PanelContainer) to enforce fixed height.
/// </summary>
public partial class TopBar : Control
{
    public const int BarHeight = 130;

    private Label _moneyAmount = null!;
    private Label _moneyDelta = null!;
    private Label _foodAmount = null!;
    private Label _foodDelta = null!;
    private Label _subtitle = null!;
    private ResearchStrip _researchStrip = null!;
    private readonly Dictionary<PrecursorColor, FactionResourceBox> _factionBoxes = new();

    /// <summary>Expose the research strip so MainScene can wire it to its research state.</summary>
    public ResearchStrip ResearchStrip => _researchStrip;

    // Layer toggle state — kept so map renderers can read it
    public bool ShowFleets { get; private set; } = true;
    public bool ShowNebulae { get; private set; }
    public bool ShowSystems { get; private set; } = true;

    public override void _Ready()
    {
        // Fixed 110px tall, full width, top of screen
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

        // Main HBox fills the 110px height
        var main = new HBoxContainer { Name = "TopBarLayout" };
        main.SetAnchorsPreset(LayoutPreset.FullRect);
        main.AddThemeConstantOverride("separation", 0);
        AddChild(main);

        // Section 1 — Logo
        BuildLogoSection(main);
        AddVerticalDivider(main);

        // Section 2 — Money & Food
        BuildMoneyFoodSection(main);
        AddVerticalDivider(main);

        // Section 2.5 — Research strip (active project bar)
        BuildResearchStrip(main);
        AddVerticalDivider(main);

        // Section 3 — Faction resource boxes
        BuildFactionBoxes(main);

        // Section 4 — Exit button
        BuildExitButton(main);

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
        logo.CustomMinimumSize = new Vector2(240, 0);
        logo.SizeFlagsVertical = SizeFlags.ExpandFill;
        parent.AddChild(logo);

        var logoVBox = new VBoxContainer();
        logoVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        logoVBox.OffsetLeft = 20;
        logoVBox.OffsetRight = -8;
        logoVBox.Alignment = BoxContainer.AlignmentMode.Center;
        logoVBox.AddThemeConstantOverride("separation", 2);
        logo.AddChild(logoVBox);

        var title = new Label { Text = "DERELICT EMPIRES" };
        UIFonts.Style(title, UIFonts.Title, UIFonts.TitleSize, UIColors.TextBright);
        logoVBox.AddChild(title);

        // Cyan underline accent (glow effect via wide color rect)
        var underline = new ColorRect();
        underline.CustomMinimumSize = new Vector2(160, 3);
        underline.Color = new Color(UIColors.Accent, 0.7f);
        logoVBox.AddChild(underline);

        // Subtitle showing empire info (updated in _Process)
        _subtitle = new Label { Text = "" };
        UIFonts.Style(_subtitle, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
        logoVBox.AddChild(_subtitle);
    }

    private void BuildMoneyFoodSection(HBoxContainer parent)
    {
        var section = new PanelContainer();
        section.CustomMinimumSize = new Vector2(180, 0);
        section.SizeFlagsVertical = SizeFlags.ExpandFill;
        var sectionStyle = new StyleBoxFlat();
        sectionStyle.BgColor = new Color(10 / 255f, 14 / 255f, 24 / 255f, 0.6f);
        sectionStyle.ContentMarginLeft = 12;
        sectionStyle.ContentMarginRight = 12;
        sectionStyle.SetBorderWidthAll(0);
        sectionStyle.SetCornerRadiusAll(0);
        section.AddThemeStyleboxOverride("panel", sectionStyle);
        parent.AddChild(section);

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 6);
        section.AddChild(vbox);

        // Money row
        var moneyRow = new HBoxContainer();
        moneyRow.AddThemeConstantOverride("separation", 8);
        moneyRow.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(moneyRow);

        var moneyIcon = new MoneyIcon();
        moneyIcon.CustomMinimumSize = new Vector2(22, 22);
        moneyIcon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        moneyRow.AddChild(moneyIcon);

        var moneyVBox = new VBoxContainer();
        moneyVBox.AddThemeConstantOverride("separation", 0);
        moneyRow.AddChild(moneyVBox);

        _moneyAmount = new Label { Text = "125,430" };
        UIFonts.Style(_moneyAmount, UIFonts.Main, UIFonts.NormalSize, Colors.White);
        moneyVBox.AddChild(_moneyAmount);

        _moneyDelta = new Label { Text = "(+2,450)" };
        UIFonts.Style(_moneyDelta, UIFonts.Main, UIFonts.SmallSize, UIColors.DeltaPosBright);
        moneyVBox.AddChild(_moneyDelta);

        // Food row
        var foodRow = new HBoxContainer();
        foodRow.AddThemeConstantOverride("separation", 8);
        foodRow.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(foodRow);

        var foodIcon = new FoodIcon();
        foodIcon.CustomMinimumSize = new Vector2(22, 22);
        foodIcon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        foodRow.AddChild(foodIcon);

        var foodVBox = new VBoxContainer();
        foodVBox.AddThemeConstantOverride("separation", 0);
        foodRow.AddChild(foodVBox);

        _foodAmount = new Label { Text = "8,500" };
        UIFonts.Style(_foodAmount, UIFonts.Main, UIFonts.NormalSize, Colors.White);
        foodVBox.AddChild(_foodAmount);

        _foodDelta = new Label { Text = "(+180)" };
        UIFonts.Style(_foodDelta, UIFonts.Main, UIFonts.SmallSize, UIColors.DeltaPosBright);
        foodVBox.AddChild(_foodDelta);
    }

    private void BuildResearchStrip(HBoxContainer parent)
    {
        var wrapper = new MarginContainer();
        wrapper.CustomMinimumSize = new Vector2(ResearchStrip.StripWidth + 16, 0);
        wrapper.SizeFlagsVertical = SizeFlags.ExpandFill;
        wrapper.AddThemeConstantOverride("margin_left", 8);
        wrapper.AddThemeConstantOverride("margin_right", 8);
        wrapper.AddThemeConstantOverride("margin_top", 8);
        wrapper.AddThemeConstantOverride("margin_bottom", 8);
        parent.AddChild(wrapper);

        _researchStrip = new ResearchStrip { Name = "ResearchStrip" };
        wrapper.AddChild(_researchStrip);
    }

    private void BuildFactionBoxes(HBoxContainer parent)
    {
        var container = new HBoxContainer();
        container.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        container.SizeFlagsVertical = SizeFlags.ExpandFill;
        container.AddThemeConstantOverride("separation", 6); // gap between faction boxes
        container.Alignment = BoxContainer.AlignmentMode.Center;
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
        }
    }

    private static void BuildExitButton(HBoxContainer parent)
    {
        var section = new Control();
        section.CustomMinimumSize = new Vector2(60, 0);
        section.SizeFlagsVertical = SizeFlags.ExpandFill;
        parent.AddChild(section);

        var btn = new Button { Text = "\u2715" }; // ✕
        btn.CustomMinimumSize = new Vector2(44, 44);
        btn.SetAnchorsPreset(LayoutPreset.Center);
        btn.OffsetLeft = -22;
        btn.OffsetRight = 22;
        btn.OffsetTop = -22;
        btn.OffsetBottom = 22;

        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = new Color(0.4f, 0.08f, 0.08f, 0.6f);
        normalStyle.SetBorderWidthAll(1);
        normalStyle.BorderColor = new Color(0.6f, 0.15f, 0.15f, 0.5f);
        normalStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", normalStyle);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = new Color(0.6f, 0.1f, 0.1f, 0.8f);
        hoverStyle.SetBorderWidthAll(1);
        hoverStyle.BorderColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        hoverStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);
        btn.AddThemeStyleboxOverride("focus", normalStyle);

        UIFonts.StyleButton(btn, UIFonts.Title, UIFonts.TitleSize, new Color(0.9f, 0.3f, 0.3f));
        btn.Pressed += () => btn.GetTree().Quit();
        section.AddChild(btn);
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
        _moneyAmount.Text = FormatCredits(credits);

        // Update subtitle with empire info
        if (_subtitle.Text.Length == 0)
            _subtitle.Text = $"{empire.Affinity}, {empire.Origin}, {empire.Name}";
    }

    /// <summary>Push income data to all faction boxes.</summary>
    public void UpdateIncome(Dictionary<string, float> income)
    {
        foreach (var box in _factionBoxes.Values)
            box.UpdateIncome(income);
    }

    private static string FormatCredits(long amount)
    {
        return $"{amount:N0}";
    }
}

/// <summary>Gold circle icon for the money display.</summary>
public partial class MoneyIcon : Control
{
    public override void _Draw()
    {
        float r = Mathf.Min(Size.X, Size.Y) / 2f;
        var center = Size / 2f;
        DrawCircle(center, r, UIColors.MoneyText);
        DrawCircle(center, r * 0.6f, new Color(0.6f, 0.5f, 0.0f, 0.5f));
    }
}

/// <summary>Brown circle icon for the food display.</summary>
public partial class FoodIcon : Control
{
    public override void _Draw()
    {
        float r = Mathf.Min(Size.X, Size.Y) / 2f;
        var center = Size / 2f;
        DrawCircle(center, r, UIColors.FoodText);
        DrawCircle(center, r * 0.6f, new Color(0.5f, 0.35f, 0.15f, 0.5f));
    }
}
