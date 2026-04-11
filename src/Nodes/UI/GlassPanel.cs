using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Applies the tarnished glass material to any PanelContainer.
/// Call GlassPanel.Apply(panel) to set StyleBoxFlat + optional overlays.
/// Overlays render BEHIND content (negative ZIndex).
/// </summary>
public static class GlassPanel
{
    private static Texture2D? _tarnishTex;
    private static Texture2D? _grainTex;
    private static bool _tarnishLoaded;
    private static bool _grainLoaded;

    /// <summary>
    /// Apply the standard glass panel look to a PanelContainer.
    /// </summary>
    public static void Apply(PanelContainer panel, bool enableBlur = false)
    {
        var style = new StyleBoxFlat();
        style.BgColor = enableBlur ? UIColors.GlassDark : UIColors.GlassDarkFlat;
        style.SetBorderWidthAll(1);
        style.BorderColor = UIColors.BorderBright;
        style.SetCornerRadiusAll(0);
        panel.AddThemeStyleboxOverride("panel", style);

        // Overlays are added first and use ShowBehindParent so content renders on top
        AddTarnishOverlay(panel);
        AddGrainOverlay(panel);
    }

    /// <summary>Create a StyleBoxFlat for button normal state.</summary>
    public static StyleBoxFlat ButtonNormal()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(16 / 255f, 28 / 255f, 48 / 255f, 0.70f);
        style.SetBorderWidthAll(1);
        style.BorderColor = UIColors.BorderDim;
        style.SetCornerRadiusAll(0);
        return style;
    }

    /// <summary>Create a StyleBoxFlat for button hover state.</summary>
    public static StyleBoxFlat ButtonHover()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.15f);
        style.SetBorderWidthAll(1);
        style.BorderColor = UIColors.BorderBright;
        style.SetCornerRadiusAll(0);
        return style;
    }

    /// <summary>Create a StyleBoxFlat for button pressed/active state.</summary>
    public static StyleBoxFlat ButtonPressed()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.22f);
        style.SetBorderWidthAll(1);
        style.BorderColor = UIColors.Accent;
        style.SetCornerRadiusAll(0);
        return style;
    }

    /// <summary>Create a StyleBoxFlat for primary action button (e.g. SEND FLEET).</summary>
    public static StyleBoxFlat ButtonPrimary()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.16f);
        style.SetBorderWidthAll(1);
        style.BorderColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.45f);
        style.SetCornerRadiusAll(0);
        return style;
    }

    /// <summary>Apply full button styling (normal/hover/pressed) to a Button.</summary>
    public static void StyleButton(Button button, bool primary = false)
    {
        button.AddThemeStyleboxOverride("normal", primary ? ButtonPrimary() : ButtonNormal());
        button.AddThemeStyleboxOverride("hover", ButtonHover());
        button.AddThemeStyleboxOverride("pressed", ButtonPressed());
        button.AddThemeStyleboxOverride("focus", ButtonHover());
    }

    private static void AddTarnishOverlay(PanelContainer panel)
    {
        var tex = LoadTarnish();
        if (tex == null) return;

        var rect = new TextureRect
        {
            Texture = tex,
            StretchMode = TextureRect.StretchModeEnum.Tile,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SelfModulate = new Color(1, 1, 1, 0.15f),
            ShowBehindParent = true
        };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(rect);
    }

    private static void AddGrainOverlay(PanelContainer panel)
    {
        var tex = LoadGrain();
        if (tex == null) return;

        var rect = new TextureRect
        {
            Texture = tex,
            StretchMode = TextureRect.StretchModeEnum.Tile,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SelfModulate = new Color(1, 1, 1, 0.03f),
            ShowBehindParent = true
        };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(rect);
    }

    private static Texture2D? LoadTarnish()
    {
        if (_tarnishLoaded) return _tarnishTex;
        _tarnishLoaded = true;
        _tarnishTex = LoadTexture("res://assets/textures/tarnish_mask.png");
        return _tarnishTex;
    }

    private static Texture2D? LoadGrain()
    {
        if (_grainLoaded) return _grainTex;
        _grainLoaded = true;
        _grainTex = LoadTexture("res://assets/textures/grain_noise.png");
        return _grainTex;
    }

    private static Texture2D? LoadTexture(string path)
    {
        if (!FileAccess.FileExists(path)) return null;
        var image = new Image();
        var err = image.Load(path);
        if (err != Error.Ok) return null;
        return ImageTexture.CreateFromImage(image);
    }
}
