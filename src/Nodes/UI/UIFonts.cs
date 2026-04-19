using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Two-font system:
///   Main  = B612 Mono Bold — body/data/labels at 14 (normal) or 12 (small). 12 is the floor.
///   Title = Exo 2 SemiBold — names that need to stand out (fleet/POI/colony/system). Always 16.
/// All legacy property names (Mono*, Rajdhani*, Exo2Bold/Medium) alias to one of the two above
/// so existing call sites compile unchanged; sizes are bumped to the new scheme at each call site.
/// </summary>
public static class UIFonts
{
    public const int TitleSize  = 16;
    public const int NormalSize = 14;
    public const int SmallSize  = 12;

    private static Font? _exo2SemiBold;
    private static FontFile? _main;

    public static Font? Title => _exo2SemiBold ??= LoadVariation("res://assets/fonts/Exo2-Variable.ttf", 600);
    public static Font? Main  => _main         ??= LoadDynamic("res://assets/fonts/B612Mono-Bold.ttf");

    // Legacy aliases — all collapse to the two-font system.
    public static Font? Exo2SemiBold           => Title;
    public static Font? Exo2Bold               => Title;
    public static Font? Exo2Medium             => Title;
    public static Font? Mono                   => Main;
    public static Font? MonoMedium             => Main;
    public static Font? MonoTracked            => Main;
    public static Font? MonoMediumTracked      => Main;
    public static Font? RajdhaniRegular        => Main;
    public static Font? RajdhaniMedium         => Main;
    public static Font? RajdhaniSemiBold       => Main;
    public static Font? RajdhaniMediumTracked  => Main;

    /// <summary>Role-based label styling. Three real roles (Title/Normal/Small); legacy names alias to them.</summary>
    public enum Role
    {
        Title, Normal, Small,
        // Legacy aliases — map to one of the three above.
        TitleLarge, TitleMedium,
        BodyPrimary, BodySecondary,
        UILabel,
        DataLarge, DataMedium, DataSmall,
        StatusBadge,
        Micro,
    }

    private static (Font? font, int size, Color color) ResolveRole(Role role) => role switch
    {
        Role.Title or Role.TitleLarge or Role.TitleMedium
            => (Title, TitleSize, UIColors.TextBright),
        Role.Normal or Role.BodyPrimary or Role.DataLarge or Role.DataMedium
            => (Main, NormalSize, UIColors.TextBody),
        Role.Small or Role.BodySecondary or Role.UILabel or Role.DataSmall or Role.StatusBadge or Role.Micro
            => (Main, SmallSize, UIColors.TextDim),
        _   => (Main, NormalSize, UIColors.TextBody),
    };

    public static void StyleRole(Label label, Role role, Color? colorOverride = null)
    {
        var spec = ResolveRole(role);
        Style(label, spec.font, spec.size, colorOverride ?? spec.color);
    }

    public static void StyleButtonRole(Button button, Role role, Color? colorOverride = null)
    {
        var spec = ResolveRole(role);
        StyleButton(button, spec.font, spec.size, colorOverride ?? spec.color);
    }

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

    public static void StyleButton(Button button, Font? font, int size, Color color)
    {
        if (font != null)
            button.AddThemeFontOverride("font", font);
        button.AddThemeFontSizeOverride("font_size", size);
        button.AddThemeColorOverride("font_color", color);
    }

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

        var font = new FontFile { Data = data };
        font.Hinting = TextServer.Hinting.Normal;
        font.ForceAutohinter = false;  // B612 Mono has professional built-in hinting
        font.Antialiasing = TextServer.FontAntialiasing.Gray;
        font.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
        font.GenerateMipmaps = false;
        return font;
    }

    private static Font? LoadVariation(string path, int weight)
    {
        var baseFont = LoadDynamic(path);
        if (baseFont == null) return null;

        var variation = new FontVariation { BaseFont = baseFont };
        variation.VariationOpentype = new Godot.Collections.Dictionary { { "wght", weight } };
        return variation;
    }
}
