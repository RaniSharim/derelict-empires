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

        // Crisp rendering for small HUD text
        font.Hinting = TextServer.Hinting.Normal;
        font.Antialiasing = TextServer.FontAntialiasing.Gray;
        font.SubpixelPositioning = TextServer.SubpixelPositioning.OneHalf;
        font.Oversampling = 2.0f;

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
