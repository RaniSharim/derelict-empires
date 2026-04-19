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
    private static FontFile? _rajdhaniSemiBold;
    private static FontFile? _rajdhaniMedium;
    private static FontFile? _rajdhaniRegular;
    private static FontFile? _plexMono;
    private static FontFile? _plexMonoMedium;

    // Exo 2 — variable font, weight axis: 700 = Bold, 600 = SemiBold, 500 = Medium
    public static Font? Exo2Bold       => _exo2Bold       ??= LoadVariation("res://assets/fonts/Exo2-Variable.ttf", 700);
    public static Font? Exo2SemiBold   => _exo2SemiBold   ??= LoadVariation("res://assets/fonts/Exo2-Variable.ttf", 600);
    public static Font? Exo2Medium     => _exo2Medium     ??= LoadVariation("res://assets/fonts/Exo2-Variable.ttf", 500);

    // Rajdhani — static TTF files (body/UI text)
    public static Font? RajdhaniSemiBold => _rajdhaniSemiBold ??= LoadDynamic("res://assets/fonts/Rajdhani-SemiBold.ttf");
    public static Font? RajdhaniMedium   => _rajdhaniMedium   ??= LoadDynamic("res://assets/fonts/Rajdhani-Medium.ttf");
    public static Font? RajdhaniRegular  => _rajdhaniRegular  ??= LoadDynamic("res://assets/fonts/Rajdhani-Regular.ttf");

    // IBM Plex Mono — static TTF (data/numbers). `Mono` kept as the generic role name.
    public static Font? Mono         => _plexMono       ??= LoadDynamic("res://assets/fonts/IBMPlexMono-Regular.ttf");
    public static Font? MonoMedium   => _plexMonoMedium ??= LoadDynamic("res://assets/fonts/IBMPlexMono-Medium.ttf");

    // Tracked variants (letter-spaced) for ALL-CAPS UI labels / status badges.
    // SpacingGlyph in pixels — 1px at 11-12px matches the design spec for ALL-CAPS tabs and badges.
    private static Font? _rajdhaniMediumTracked;
    private static Font? _monoTracked;
    private static Font? _monoMediumTracked;

    public static Font? RajdhaniMediumTracked => _rajdhaniMediumTracked ??= CreateTracked(RajdhaniMedium, 1);
    public static Font? MonoTracked           => _monoTracked           ??= CreateTracked(Mono, 1);
    public static Font? MonoMediumTracked     => _monoMediumTracked     ??= CreateTracked(MonoMedium, 1);

    /// <summary>
    /// Role-based label styling. Preferred entry point — avoids misapplying sizes/colors.
    /// Roles map to the sizing table in references/fonts.md §5.
    /// </summary>
    public enum Role
    {
        /// <summary>Title Large — 18px Exo 2 SemiBold. Screen headers, system detail titles.</summary>
        TitleLarge,
        /// <summary>Title Medium — 14px Exo 2 SemiBold ALL-CAPS. Fleet/POI/colony names.</summary>
        TitleMedium,
        /// <summary>Body Primary — 13px Rajdhani Medium. Metadata, locations, descriptions.</summary>
        BodyPrimary,
        /// <summary>Body Secondary — 12px Rajdhani Regular. Event log, secondary details.</summary>
        BodySecondary,
        /// <summary>UI Label — 11px Rajdhani Medium ALL-CAPS tracked. Tabs, section headers, button labels.</summary>
        UILabel,
        /// <summary>Data Large — 13px IBM Plex Mono. Resource values, primary numeric display.</summary>
        DataLarge,
        /// <summary>Data Medium — 12px IBM Plex Mono. Mid-size numeric display.</summary>
        DataMedium,
        /// <summary>Data Small — 11px IBM Plex Mono. Deltas, timestamps, sub-values.</summary>
        DataSmall,
        /// <summary>Status Badge — 11px IBM Plex Mono ALL-CAPS tracked. MOVING, IDLE, COMBAT, etc.</summary>
        StatusBadge,
        /// <summary>Micro — 10px IBM Plex Mono. Tiny annotations.</summary>
        Micro,
    }

    /// <summary>
    /// Apply a predefined role to a Label. Color stays default for the role unless overridden.
    /// </summary>
    public static void StyleRole(Label label, Role role, Color? colorOverride = null)
    {
        (Font? font, int size, Color color) spec = role switch
        {
            Role.TitleLarge     => (Exo2SemiBold,             18, UIColors.TextBright),
            Role.TitleMedium    => (Exo2SemiBold,             14, UIColors.TextBright),
            Role.BodyPrimary    => (RajdhaniMedium,           13, UIColors.TextBody),
            Role.BodySecondary  => (RajdhaniRegular,          12, UIColors.TextBody),
            Role.UILabel        => (RajdhaniMediumTracked,    11, UIColors.TextDim),
            Role.DataLarge      => (Mono,                     13, UIColors.TextBody),
            Role.DataMedium     => (Mono,                     12, UIColors.TextBody),
            Role.DataSmall      => (Mono,                     11, UIColors.TextDim),
            Role.StatusBadge    => (MonoTracked,              11, UIColors.TextDim),
            Role.Micro          => (Mono,                     10, UIColors.TextDim),
            _                   => (RajdhaniMedium,           13, UIColors.TextBody),
        };
        Style(label, spec.font, spec.size, colorOverride ?? spec.color);
    }

    /// <summary>Same as StyleRole but for Buttons.</summary>
    public static void StyleButtonRole(Button button, Role role, Color? colorOverride = null)
    {
        (Font? font, int size, Color color) spec = role switch
        {
            Role.TitleMedium    => (Exo2SemiBold,             14, UIColors.TextBright),
            Role.BodyPrimary    => (RajdhaniMedium,           13, UIColors.TextBody),
            Role.UILabel        => (RajdhaniMediumTracked,    11, UIColors.TextLabel),
            Role.StatusBadge    => (MonoTracked,              11, UIColors.TextLabel),
            _                   => (RajdhaniMediumTracked,    11, UIColors.TextLabel),
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
        // Subpixel positioning disabled per swap spec (keeps kerning on exact pixel boundaries).
        // Normal is Godot's maximum hinting (maps to FreeType "Full") — snaps strokes to the pixel grid.
        font.Hinting = TextServer.Hinting.Normal;
        font.ForceAutohinter = true;
        font.Antialiasing = TextServer.FontAntialiasing.Gray;
        font.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
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
