using Godot;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Upper-right-of-panel chip showing projected wreckage/components from the current battle.
/// Live counter of wrecks + debris, with a hover rule explaining overkill → debris degradation.
/// Updates on BattleTick via the owning panel.
/// </summary>
public partial class SalvageProjectionChip : PanelContainer
{
    private Label _summaryLabel = null!;
    private Label _detailLabel = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 48);
        TooltipText = "Sustained damage on ships below 10% HP converts wrecks to debris. Stop focus-fire to preserve wrecks.";

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.45f),
            BorderColor = UIColors.BorderMid,
        };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(0);
        bg.ContentMarginLeft = 10;
        bg.ContentMarginRight = 10;
        bg.ContentMarginTop = 6;
        bg.ContentMarginBottom = 6;
        AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        AddChild(col);

        _summaryLabel = new Label { Text = "\u2295 SALVAGE PROJECTION" };
        UIFonts.StyleRole(_summaryLabel, UIFonts.Role.Small, UIColors.TextLabel);
        col.AddChild(_summaryLabel);

        _detailLabel = new Label { Text = "0 wrecks, 0 debris" };
        UIFonts.StyleRole(_detailLabel, UIFonts.Role.Small);
        col.AddChild(_detailLabel);
    }

    public void UpdateFrom(int wrecks, int debris, int projectedComponents)
    {
        _detailLabel.Text = $"{wrecks} wrecks, {debris} debris · +{projectedComponents} projected";
    }
}
