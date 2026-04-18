using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Lazy-loaded font references. All fonts live in res://assets/fonts/.
/// Loads TTF data directly via FileAccess (no Godot import pipeline needed).
/// Exo 2 is a variable font — SemiBold/Medium are FontVariation instances.
/// </summary>
public static class UIFonts
{
    private static Font? _exo2Bold;
    private static Font? _exo2SemiBold;
    private static Font? _exo2Medium;
    private static FontFile? _barlowSemiBold;
    private static FontFile? _barlowMedium;
    private static FontFile? _barlowRegular;
    private static FontFile? _shareTechMono;

    // Exo 2 — variable font, weight axis: 700 = Bold, 600 = SemiBold, 500 = Medium
    public static Font? Exo2Bold       => _exo2Bold       ??= LoadVariation("res://assets/fonts/Exo2-Variable.ttf", 700);
    public static Font? Exo2SemiBold   => _exo2SemiBold   ??= LoadVariation("res://assets/fonts/Exo2-Variable.ttf", 600);
    public static Font? Exo2Medium     => _exo2Medium     ??= LoadVariation("res://assets/fonts/Exo2-Variable.ttf", 500);

    // Barlow Condensed — static TTF files
    public static Font? BarlowSemiBold => _barlowSemiBold ??= LoadDynamic("res://assets/fonts/BarlowCondensed-SemiBold.ttf");
    public static Font? BarlowMedium   => _barlowMedium   ??= LoadDynamic("res://assets/fonts/BarlowCondensed-Medium.ttf");
    public static Font? BarlowRegular  => _barlowRegular  ??= LoadDynamic("res://assets/fonts/BarlowCondensed-Regular.ttf");

    // Share Tech Mono — static TTF
    public static Font? ShareTechMono  => _shareTechMono  ??= LoadDynamic("res://assets/fonts/ShareTechMono-Regular.ttf");

    // Tracked variants (letter-spaced) for ALL-CAPS UI labels / status badges.
    // SpacingGlyph in pixels — values > 0 make small text feel stretched at 10px, so keep at 0.
    // The slot is kept as a placeholder so callers can pick the tracked role; bump if/when sizes grow.
    private static Font? _barlowMediumTracked;
    private static Font? _shareTechMonoTracked;

    public static Font? BarlowMediumTracked => _barlowMediumTracked ??= CreateTracked(BarlowMedium, 0);
    public static Font? ShareTechMonoTracked => _shareTechMonoTracked ??= CreateTracked(ShareTechMono, 0);

    /// <summary>
    /// Role-based label styling. Preferred entry point — avoids misapplying sizes/colors.
    /// Roles map to the sizing table in references/fonts.md §5.
    /// </summary>
    public enum Role
    {
        /// <summary>Title Large — 16px Exo 2 SemiBold. Screen headers, system detail titles.</summary>
        TitleLarge,
        /// <summary>Title Medium — 13px Exo 2 SemiBold ALL-CAPS. Fleet/POI/colony names.</summary>
        TitleMedium,
        /// <summary>Body Primary — 11px Barlow Medium. Metadata, locations, descriptions.</summary>
        BodyPrimary,
        /// <summary>Body Secondary — 10px Barlow Regular. Event log, secondary details.</summary>
        BodySecondary,
        /// <summary>UI Label — 10px Barlow Medium ALL-CAPS tracked. Tabs, section headers, button labels.</summary>
        UILabel,
        /// <summary>Data Large — 12px Share Tech Mono. Resource values, primary numeric display.</summary>
        DataLarge,
        /// <summary>Data Small — 10px Share Tech Mono. Deltas, timestamps, sub-values.</summary>
        DataSmall,
        /// <summary>Status Badge — 10px Share Tech Mono ALL-CAPS tracked. MOVING, IDLE, COMBAT, etc.</summary>
        StatusBadge,
    }

    /// <summary>
    /// Apply a predefined role to a Label. Color stays default for the role unless overridden.
    /// </summary>
    public static void StyleRole(Label label, Role role, Color? colorOverride = null)
    {
        (Font? font, int size, Color color) spec = role switch
        {
            Role.TitleLarge     => (Exo2SemiBold,           16, UIColors.TextBright),
            Role.TitleMedium    => (Exo2SemiBold,           13, UIColors.TextBright),
            Role.BodyPrimary    => (BarlowMedium,           11, UIColors.TextBody),
            Role.BodySecondary  => (BarlowRegular,          10, UIColors.TextBody),
            Role.UILabel        => (BarlowMediumTracked,    10, UIColors.TextDim),
            Role.DataLarge      => (ShareTechMono,          12, UIColors.TextBody),
            Role.DataSmall      => (ShareTechMono,          10, UIColors.TextDim),
            Role.StatusBadge    => (ShareTechMonoTracked,   10, UIColors.TextDim),
            _                   => (BarlowMedium,           11, UIColors.TextBody),
        };
        Style(label, spec.font, spec.size, colorOverride ?? spec.color);
    }

    /// <summary>Same as StyleRole but for Buttons.</summary>
    public static void StyleButtonRole(Button button, Role role, Color? colorOverride = null)
    {
        (Font? font, int size, Color color) spec = role switch
        {
            Role.TitleMedium    => (Exo2SemiBold,           13, UIColors.TextBright),
            Role.BodyPrimary    => (BarlowMedium,           11, UIColors.TextBody),
            Role.UILabel        => (BarlowMediumTracked,    10, UIColors.TextLabel),
            Role.StatusBadge    => (ShareTechMonoTracked,   10, UIColors.TextLabel),
            _                   => (BarlowMediumTracked,    10, UIColors.TextLabel),
        };
        StyleButton(button, spec.font, spec.size, colorOverride ?? spec.color);
    }

    private static FontVariation? CreateTracked(Font? baseFont, int spacingPx)
    {
        if (baseFont == null) return null;
        var fv = new FontVariation();
        fv.BaseFont = baseFont;
        fv.SpacingGlyph = spacingPx;
        return fv;
    }

    /// <summary>
    /// Apply font + size + color to a Label in one call.
    /// Optional shadow for readability on colored backgrounds.
    /// </summary>
    public static void Style(Label label, Font? font, int size, Color color, bool shadow = false)
    {
        if (font != null)
            label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        if (shadow)
        {
            label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.6f));
            label.AddThemeConstantOverride("shadow_offset_x", 1);
            label.AddThemeConstantOverride("shadow_offset_y", 1);
        }
    }

    /// <summary>
    /// Apply font + size + color to a Button's label in one call.
    /// </summary>
    public static void StyleButton(Button button, Font? font, int size, Color color)
    {
        if (font != null)
            button.AddThemeFontOverride("font", font);
        button.AddThemeFontSizeOverride("font_size", size);
        button.AddThemeColorOverride("font_color", color);
    }

    /// <summary>Load a TTF file directly from disk, bypassing the import system.</summary>
    private static FontFile? LoadDynamic(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PushWarning($"UIFonts: font file not found at {path}");
            return null;
        }

        var data = FileAccess.GetFileAsBytes(path);
        if (data == null || data.Length == 0)
        {
            GD.PushWarning($"UIFonts: failed to read font data from {path}");
            return null;
        }

        var font = new FontFile();
        font.Data = data;

        // Crisp rendering for small HUD text — per godot-4x-csharp/references/fonts.md §2.1.
        // Full hinting snaps strokes to the pixel grid (critical below 12px).
        // OneHalf subpixel positioning keeps kerning sharp without uneven spacing.
        // Normal is Godot's maximum hinting (maps to FreeType "Full") — snaps strokes to the pixel grid.
        font.Hinting = TextServer.Hinting.Normal;
        font.ForceAutohinter = true;
        font.Antialiasing = TextServer.FontAntialiasing.Gray;
        font.SubpixelPositioning = TextServer.SubpixelPositioning.OneHalf;
        font.GenerateMipmaps = false;

        return font;
    }

    private static Font? LoadVariation(string path, int weight)
    {
        var baseFont = LoadDynamic(path);
        if (baseFont == null) return null;

        var variation = new FontVariation();
        variation.BaseFont = baseFont;
        variation.VariationOpentype = new Godot.Collections.Dictionary
        {
            { "wght", weight }
        };
        return variation;
    }
}
