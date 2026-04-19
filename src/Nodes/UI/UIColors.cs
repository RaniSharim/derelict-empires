using Godot;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Centralized color palette for all UI elements.
/// Precursor colors live on a 4-tone scale: Bright (vivid/hover), Normal (default glow),
/// Dim (desaturated/unfocused), Bg (translucent panel fill). Use <see cref="GetPrecursor"/>
/// for enum-driven lookups; the named `{Color}{Tone}` fields for direct use.
/// </summary>
public static class UIColors
{
    // Base palette (§2 tokens)
    public static readonly Color BgDeep       = new("#060a14"); // --bg-void
    public static readonly Color GlassDark    = new(6 / 255f, 8 / 255f, 18 / 255f, 0.94f); // --panel-base
    public static readonly Color GlassDarkFlat = new(6 / 255f, 8 / 255f, 18 / 255f, 0.96f); // no-blur fallback
    public static readonly Color BorderDim    = new(80 / 255f, 120 / 255f, 180 / 255f, 0.25f);
    public static readonly Color BorderMid    = new(80 / 255f, 120 / 255f, 180 / 255f, 0.38f);
    public static readonly Color BorderBright = new(80 / 255f, 120 / 255f, 180 / 255f, 0.50f);

    // Text hierarchy
    public static readonly Color TextFaint    = new("#3a4a5c");
    public static readonly Color TextDim      = new("#667a8c");
    public static readonly Color TextBody     = new("#99aabb");
    public static readonly Color TextLabel    = new("#b8d2de");
    public static readonly Color TextBright   = new("#d8dce6");

    // Accent colors (UI semantics, not per-precursor)
    public static readonly Color Accent       = new("#44aaff");
    public static readonly Color AccentGreen  = new("#4caf50");
    public static readonly Color AccentRed    = new("#e85545");
    public static readonly Color AccentGold   = new("#ffcc44");
    public static readonly Color AccentPurple = new("#b366e8");
    public static readonly Color AccentOrange = new("#e8883a");
    public static readonly Color AccentOlive  = new("#8a8a3c");

    // ───── Precursor presets: 5 colors × 4 tones ─────
    // Adjust these in one place and the whole UI follows.

    public static readonly Color RedBright    = new("#ff6a55");
    public static readonly Color RedNormal    = new("#f04030");
    public static readonly Color RedDim       = new("#8a2a1e");
    public static readonly Color RedBg        = new(140 / 255f,  28 / 255f,  16 / 255f, 0.28f);

    public static readonly Color BlueBright   = new("#77bbff");
    public static readonly Color BlueNormal   = new("#2288ee");
    public static readonly Color BlueDim      = new("#144477");
    public static readonly Color BlueBg       = new( 16 / 255f,  58 / 255f, 128 / 255f, 0.28f);

    public static readonly Color GreenBright  = new("#66dd77");
    public static readonly Color GreenNormal  = new("#22bb44");
    public static readonly Color GreenDim     = new("#0f6622");
    public static readonly Color GreenBg      = new( 16 / 255f,  88 / 255f,  28 / 255f, 0.28f);

    public static readonly Color GoldBright   = new("#ffd777");
    public static readonly Color GoldNormal   = new("#ddaa22");
    public static readonly Color GoldDim      = new("#7a5e11");
    public static readonly Color GoldBg       = new(118 / 255f,  88 / 255f,   8 / 255f, 0.28f);

    public static readonly Color PurpleBright = new("#cc88ee");
    public static readonly Color PurpleNormal = new("#9944dd");
    public static readonly Color PurpleDim    = new("#55227a");
    public static readonly Color PurpleBg     = new( 68 / 255f,  18 / 255f, 108 / 255f, 0.28f);

    // Legacy aliases — existing call sites use *Glow; route them to the Normal preset.
    public static readonly Color RedGlow    = RedNormal;
    public static readonly Color BlueGlow   = BlueNormal;
    public static readonly Color GreenGlow  = GreenNormal;
    public static readonly Color GoldGlow   = GoldNormal;
    public static readonly Color PurpleGlow = PurpleNormal;

    public enum Tone { Bright, Normal, Dim, Bg }

    /// <summary>Look up a precursor color by tone. TextDim is returned for unknown precursors.</summary>
    public static Color GetPrecursor(PrecursorColor color, Tone tone) => (color, tone) switch
    {
        (PrecursorColor.Red,    Tone.Bright) => RedBright,
        (PrecursorColor.Red,    Tone.Normal) => RedNormal,
        (PrecursorColor.Red,    Tone.Dim)    => RedDim,
        (PrecursorColor.Red,    Tone.Bg)     => RedBg,
        (PrecursorColor.Blue,   Tone.Bright) => BlueBright,
        (PrecursorColor.Blue,   Tone.Normal) => BlueNormal,
        (PrecursorColor.Blue,   Tone.Dim)    => BlueDim,
        (PrecursorColor.Blue,   Tone.Bg)     => BlueBg,
        (PrecursorColor.Green,  Tone.Bright) => GreenBright,
        (PrecursorColor.Green,  Tone.Normal) => GreenNormal,
        (PrecursorColor.Green,  Tone.Dim)    => GreenDim,
        (PrecursorColor.Green,  Tone.Bg)     => GreenBg,
        (PrecursorColor.Gold,   Tone.Bright) => GoldBright,
        (PrecursorColor.Gold,   Tone.Normal) => GoldNormal,
        (PrecursorColor.Gold,   Tone.Dim)    => GoldDim,
        (PrecursorColor.Gold,   Tone.Bg)     => GoldBg,
        (PrecursorColor.Purple, Tone.Bright) => PurpleBright,
        (PrecursorColor.Purple, Tone.Normal) => PurpleNormal,
        (PrecursorColor.Purple, Tone.Dim)    => PurpleDim,
        (PrecursorColor.Purple, Tone.Bg)     => PurpleBg,
        _ => TextDim,
    };

    // Legacy helpers — route to the preset system.
    public static Color GetFactionGlow(PrecursorColor color) => GetPrecursor(color, Tone.Normal);
    public static Color GetFactionBg(PrecursorColor color)   => GetPrecursor(color, Tone.Bg);

    // Status
    public static readonly Color Alert    = AccentRed;
    public static readonly Color Moving   = AccentGold;
    public static readonly Color DeltaPos = AccentGreen;
    public static readonly Color DeltaNeg = AccentRed;

    // Bright delta (on colored cell backgrounds)
    public static readonly Color DeltaPosBright = new("#55ff66");
    public static readonly Color DeltaNegBright = new("#ff4444");

    // Credits / Money / Food
    public static readonly Color CreditText = new("#ccd898");
    public static readonly Color CreditBg   = new(90 / 255f, 110 / 255f, 60 / 255f, 0.08f);
    public static readonly Color MoneyText  = new("#ffd700");
    public static readonly Color FoodText   = new("#d4a574");
}
