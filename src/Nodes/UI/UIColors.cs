using Godot;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Centralized color palette for all UI elements.
/// Values from ui_instructions.md — do not scatter colors into individual panels.
/// </summary>
public static class UIColors
{
    // Base palette
    public static readonly Color BgDeep       = new("#040810");
    public static readonly Color GlassDark    = new(4 / 255f, 8 / 255f, 16 / 255f, 0.88f);
    public static readonly Color GlassDarkFlat = new(4 / 255f, 8 / 255f, 16 / 255f, 0.92f); // no-blur fallback
    public static readonly Color BorderDim    = new(60 / 255f, 110 / 255f, 160 / 255f, 0.30f);
    public static readonly Color BorderBright = new(90 / 255f, 160 / 255f, 230 / 255f, 0.50f);
    public static readonly Color TextFaint    = new("#4a6880");
    public static readonly Color TextDim      = new("#7b9eb5");
    public static readonly Color TextBody     = new("#88aabb");
    public static readonly Color TextLabel    = new("#b8d2de");
    public static readonly Color TextBright   = new("#e0eef6");
    public static readonly Color Accent       = new("#2288ee");

    // Faction glow colors (text, active icons, owned nodes)
    public static readonly Color RedGlow    = new("#f04030");
    public static readonly Color BlueGlow   = new("#2288ee");
    public static readonly Color GreenGlow  = new("#22bb44");
    public static readonly Color GoldGlow   = new("#ddaa22");
    public static readonly Color PurpleGlow = new("#9944dd");

    // Faction background tints (panel fills)
    public static readonly Color RedBg    = new(140 / 255f, 28 / 255f, 16 / 255f, 0.28f);
    public static readonly Color BlueBg   = new(16 / 255f, 58 / 255f, 128 / 255f, 0.28f);
    public static readonly Color GreenBg  = new(16 / 255f, 88 / 255f, 28 / 255f, 0.28f);
    public static readonly Color GoldBg   = new(118 / 255f, 88 / 255f, 8 / 255f, 0.28f);
    public static readonly Color PurpleBg = new(68 / 255f, 18 / 255f, 108 / 255f, 0.28f);

    // Status
    public static readonly Color Alert    = new("#ff5540");
    public static readonly Color Moving   = new("#ffcc44");
    public static readonly Color DeltaPos = new("#66dd88");
    public static readonly Color DeltaNeg = new("#ff6655");

    // Credits display
    public static readonly Color CreditText = new("#ccd898");
    public static readonly Color CreditBg   = new(90 / 255f, 110 / 255f, 60 / 255f, 0.08f);

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
