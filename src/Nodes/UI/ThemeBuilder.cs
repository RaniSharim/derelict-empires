using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Project-wide Theme factory. Produces the default styles consumed by every Control
/// when scenes inherit the root Theme. Phase 2/3 panels rely on these defaults to
/// stop hand-rolling StyleBoxes per control.
///
/// Fonts deliberately stay out of the Theme — <see cref="UIFonts"/> loads .ttf files
/// via FileAccess (bypassing the import pipeline) for precise hinting/antialias control.
/// Per-Label styling continues to flow through <c>UIFonts.Style*()</c>; the Theme only
/// carries colors, styleboxes, sizes, and constants.
///
/// Usage: call <see cref="Apply"/> once, very early in MainScene._Ready, before any
/// UI Control is instantiated. Children inherit the root Theme automatically.
/// </summary>
public static class ThemeBuilder
{
    public const string ThemeResourcePath = "res://resources/ui/theme.tres";

    public static Theme Build()
    {
        var theme = new Theme();

        BuildButton(theme);
        BuildPanel(theme);
        BuildLabel(theme);
        BuildLineEdit(theme);
        BuildTabContainer(theme);
        BuildProgressBar(theme);
        BuildScrollContainer(theme);

        return theme;
    }

    /// <summary>Applies the project-wide Theme to the root Window so every Control inherits it.</summary>
    public static void Apply(SceneTree tree)
    {
        var theme = ResourceLoader.Exists(ThemeResourcePath)
            ? ResourceLoader.Load<Theme>(ThemeResourcePath)
            : Build();
        tree.Root.Theme = theme;
    }

    /// <summary>One-shot: build the theme and save it to disk as a .tres. Call from a tool
    /// or debug command. The resulting file is committed and referenced by project.godot.</summary>
    public static Error SaveToDisk(string path = ThemeResourcePath)
    {
        var dir = path[..path.LastIndexOf('/')];
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(dir));
        return ResourceSaver.Save(Build(), path);
    }

    // ─────────────────────────────────────────────────────────────────

    private static void BuildButton(Theme theme)
    {
        const string T = "Button";
        theme.SetStylebox("normal",   T, Box(UIColors.GlassDark,     UIColors.BorderMid,    1));
        theme.SetStylebox("hover",    T, Box(UIColors.GlassDarkFlat, UIColors.BorderBright, 1));
        theme.SetStylebox("pressed",  T, Box(UIColors.GlassDarkFlat, UIColors.Accent,       1));
        theme.SetStylebox("focus",    T, Box(UIColors.GlassDark,     UIColors.Accent,       1));
        theme.SetStylebox("disabled", T, Box(UIColors.GlassDark,     UIColors.BorderDim,    1));

        theme.SetColor("font_color",          T, UIColors.TextBody);
        theme.SetColor("font_hover_color",    T, UIColors.TextBright);
        theme.SetColor("font_pressed_color",  T, UIColors.Accent);
        theme.SetColor("font_focus_color",    T, UIColors.TextBright);
        theme.SetColor("font_disabled_color", T, UIColors.TextFaint);

        theme.SetConstant("h_separation", T, 6);
    }

    private static void BuildPanel(Theme theme)
    {
        var panelBox = Box(UIColors.GlassDark, UIColors.BorderDim, 1);
        theme.SetStylebox("panel", "Panel",          panelBox);
        theme.SetStylebox("panel", "PanelContainer", panelBox);
    }

    private static void BuildLabel(Theme theme)
    {
        theme.SetColor("font_color", "Label", UIColors.TextBody);
        theme.SetFontSize("font_size", "Label", UIFonts.NormalSize);
    }

    private static void BuildLineEdit(Theme theme)
    {
        const string T = "LineEdit";
        theme.SetStylebox("normal",   T, Box(UIColors.GlassDark,     UIColors.BorderMid,    1));
        theme.SetStylebox("focus",    T, Box(UIColors.GlassDarkFlat, UIColors.Accent,       1));
        theme.SetStylebox("read_only", T, Box(UIColors.GlassDark,    UIColors.BorderDim,    1));
        theme.SetColor("font_color",            T, UIColors.TextBody);
        theme.SetColor("font_selected_color",   T, UIColors.TextBright);
        theme.SetColor("font_uneditable_color", T, UIColors.TextFaint);
        theme.SetColor("caret_color",           T, UIColors.Accent);
        theme.SetColor("selection_color",       T, new Color(UIColors.Accent.R, UIColors.Accent.G, UIColors.Accent.B, 0.30f));
    }

    private static void BuildTabContainer(Theme theme)
    {
        const string T = "TabContainer";
        theme.SetStylebox("panel",          T, Box(UIColors.GlassDark, UIColors.BorderDim, 1));
        theme.SetStylebox("tab_selected",   T, TabBox(UIColors.GlassDarkFlat, UIColors.Accent,    bottomAccent: true));
        theme.SetStylebox("tab_unselected", T, TabBox(UIColors.GlassDark,     UIColors.BorderDim, bottomAccent: false));
        theme.SetStylebox("tab_hovered",    T, TabBox(UIColors.GlassDarkFlat, UIColors.BorderBright, bottomAccent: false));
        theme.SetStylebox("tab_disabled",   T, TabBox(UIColors.GlassDark,     UIColors.BorderDim, bottomAccent: false));
        theme.SetColor("font_selected_color",   T, UIColors.Accent);
        theme.SetColor("font_unselected_color", T, UIColors.TextBody);
        theme.SetColor("font_hovered_color",    T, UIColors.TextBright);
        theme.SetColor("font_disabled_color",   T, UIColors.TextFaint);
    }

    private static void BuildProgressBar(Theme theme)
    {
        const string T = "ProgressBar";
        var bg   = Box(UIColors.GlassDarkFlat, UIColors.BorderDim, 1);
        var fill = new StyleBoxFlat { BgColor = UIColors.Accent };
        fill.SetBorderWidthAll(0);
        fill.SetCornerRadiusAll(0);
        theme.SetStylebox("background", T, bg);
        theme.SetStylebox("fill",       T, fill);
        theme.SetColor("font_color", T, UIColors.TextBright);
    }

    private static void BuildScrollContainer(Theme theme)
    {
        var empty = new StyleBoxEmpty();
        theme.SetStylebox("panel", "ScrollContainer", empty);
    }

    // ─────────────────────────────────────────────────────────────────

    private static StyleBoxFlat Box(Color bg, Color border, int width)
    {
        var box = new StyleBoxFlat { BgColor = bg, BorderColor = border };
        box.SetBorderWidthAll(width);
        box.SetCornerRadiusAll(0);
        box.ContentMarginLeft = 8;
        box.ContentMarginRight = 8;
        box.ContentMarginTop = 4;
        box.ContentMarginBottom = 4;
        return box;
    }

    private static StyleBoxFlat TabBox(Color bg, Color accent, bool bottomAccent)
    {
        var box = new StyleBoxFlat { BgColor = bg, BorderColor = accent };
        box.BorderWidthLeft = 0;
        box.BorderWidthRight = 0;
        box.BorderWidthTop = 0;
        box.BorderWidthBottom = bottomAccent ? 2 : 0;
        box.SetCornerRadiusAll(0);
        box.ContentMarginLeft = 12;
        box.ContentMarginRight = 12;
        box.ContentMarginTop = 6;
        box.ContentMarginBottom = 6;
        return box;
    }
}
