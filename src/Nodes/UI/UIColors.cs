using Godot;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Centralized color palette for all UI elements.
/// Values from ui_instructions.md §2 — do not scatter colors into individual panels.
/// </summary>
public static class UIColors
{
    // Base palette (§2 tokens)
    public static readonly Color BgDeep       = new("#060a14"); // --bg-void
    public static readonly Color GlassDark    = new(8 / 255f, 12 / 255f, 28 / 255f, 0.85f); // --panel-base
    public static readonly Color GlassDarkFlat = new(8 / 255f, 12 / 255f, 28 / 255f, 0.92f); // no-blur fallback
    public static readonly Color BorderDim    = new(80 / 255f, 120 / 255f, 180 / 255f, 0.25f); // --panel-border
    public static readonly Color BorderBright = new(80 / 255f, 120 / 255f, 180 / 255f, 0.50f);

    // Text hierarchy (§2 tokens)
    public static readonly Color TextFaint    = new("#3a4a5c"); // --text-dim (disabled, placeholder)
    public static readonly Color TextDim      = new("#667a8c"); // --text-secondary
    public static readonly Color TextBody     = new("#99aabb"); // between secondary and primary
    public static readonly Color TextLabel    = new("#b8d2de"); // label text
    public static readonly Color TextBright   = new("#d8dce6"); // --text-primary

    // Accent colors (§2 — color as meaning)
    public static readonly Color Accent       = new("#44aaff"); // --accent-cyan: UI focus/selection
    public static readonly Color AccentGreen  = new("#4caf50"); // --accent-green: positive/owned
    public static readonly Color AccentRed    = new("#e85545"); // --accent-red: negative/threat
    public static readonly Color AccentGold   = new("#ffcc44"); // --accent-gold: movement/caution
    public static readonly Color AccentPurple = new("#b366e8"); // --accent-purple: strange/rare
    public static readonly Color AccentOrange = new("#e8883a"); // --accent-orange: military
    public static readonly Color AccentOlive  = new("#8a8a3c"); // --accent-olive: environmental

    // Faction glow colors (text, active icons, owned nodes)
    public static readonly Color RedGlow    = new("#f04030");
    public static readonly Color BlueGlow   = new("#2288ee");
    public static readonly Color GreenGlow  = new("#22bb44");
    public static readonly Color GoldGlow   = new("#ddaa22");
    public static readonly Color PurpleGlow = new("#9944dd");

    // Faction background tints (panel fills — subtle, 0.28 alpha)
    public static readonly Color RedBg    = new(140 / 255f, 28 / 255f, 16 / 255f, 0.28f);
    public static readonly Color BlueBg   = new(16 / 255f, 58 / 255f, 128 / 255f, 0.28f);
    public static readonly Color GreenBg  = new(16 / 255f, 88 / 255f, 28 / 255f, 0.28f);
    public static readonly Color GoldBg   = new(118 / 255f, 88 / 255f, 8 / 255f, 0.28f);
    public static readonly Color PurpleBg = new(68 / 255f, 18 / 255f, 108 / 255f, 0.28f);

    // Status
    public static readonly Color Alert    = new("#e85545"); // matches AccentRed
    public static readonly Color Moving   = new("#ffcc44"); // matches AccentGold
    public static readonly Color DeltaPos = new("#4caf50"); // matches AccentGreen
    public static readonly Color DeltaNeg = new("#e85545"); // matches AccentRed

    // Bright delta colors (visible on colored cell backgrounds)
    public static readonly Color DeltaPosBright = new("#55ff66"); // vivid green
    public static readonly Color DeltaNegBright = new("#ff4444"); // vivid red

    // Credits display
    public static readonly Color CreditText = new("#ccd898");
    public static readonly Color CreditBg   = new(90 / 255f, 110 / 255f, 60 / 255f, 0.08f);

    // Money & Food display
    public static readonly Color MoneyText  = new("#ffd700"); // gold
    public static readonly Color FoodText   = new("#d4a574"); // light warm brown

    public static Color GetFactionGlow(PrecursorColor color) => color switch
    {
        PrecursorColor.Red    => RedGlow,
        PrecursorColor.Blue   => BlueGlow,
        PrecursorColor.Green  => GreenGlow,
        PrecursorColor.Gold   => GoldGlow,
        PrecursorColor.Purple => PurpleGlow,
        _ => TextDim
    };

    public static Color GetFactionBg(PrecursorColor color) => color switch
    {
        PrecursorColor.Red    => RedBg,
        PrecursorColor.Blue   => BlueBg,
        PrecursorColor.Green  => GreenBg,
        PrecursorColor.Gold   => GoldBg,
        PrecursorColor.Purple => PurpleBg,
        _ => GlassDark
    };
}
