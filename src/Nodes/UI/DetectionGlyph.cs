using System.Collections.Generic;
using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Renders a detection-layer glyph (sensor / signature) at a pixel size with a canonical
/// or overridden tint. Mirrors the runtime-SVG + shared-cache pattern from FactionResourceBox.
/// </summary>
public partial class DetectionGlyph : Control
{
    public enum Kind { Sensor, Signature }

    private static readonly Dictionary<string, Texture2D> _cache = new();

    public Kind GlyphKind { get; set; } = Kind.Sensor;
    public int  PixelSize { get; set; } = 12;
    public Color? TintOverride { get; set; }

    private Texture2D? _texture;

    public DetectionGlyph() { }

    public DetectionGlyph(Kind kind, int pixelSize = 12, Color? tintOverride = null)
    {
        GlyphKind = kind;
        PixelSize = pixelSize;
        TintOverride = tintOverride;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(PixelSize, PixelSize);
        SizeFlagsHorizontal = 0;                       // don't expand
        SizeFlagsVertical   = SizeFlags.ShrinkCenter;  // center vertically in HBox row
        MouseFilter = MouseFilterEnum.Ignore;
        _texture = LoadGlyph(GlyphKind, PixelSize);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_texture == null) return;
        var tint = TintOverride ?? DefaultTint(GlyphKind);
        // Draw the texture at the exact rasterization size rather than the Control's Size, so
        // an oversized HBox row can't stretch the sprite out of aspect.
        DrawTextureRect(_texture, new Rect2(Vector2.Zero, new Vector2(PixelSize, PixelSize)), false, tint);
    }

    public static Color DefaultTint(Kind kind) => kind switch
    {
        Kind.Sensor    => UIColors.SensorIcon,
        Kind.Signature => UIColors.SigIcon,
        _              => UIColors.TextDim,
    };

    private static string PathFor(Kind kind) => kind switch
    {
        Kind.Sensor    => IconMapping.SensorIcon,
        Kind.Signature => IconMapping.SignatureIcon,
        _              => IconMapping.SensorIcon,
    };

    private static Texture2D? LoadGlyph(Kind kind, int pixelSize)
    {
        var path = PathFor(kind);
        var key  = $"{path}@{pixelSize}";
        if (_cache.TryGetValue(key, out var cached)) return cached;

        if (!FileAccess.FileExists(path))
        {
            McpLog.Warn($"[DetectionGlyph] missing {path}");
            return null;
        }

        var bytes = FileAccess.GetFileAsBytes(path);
        if (bytes == null || bytes.Length == 0) return null;

        var img = new Image();
        img.LoadSvgFromBuffer(bytes, pixelSize / 512f);
        if (img.IsEmpty()) return null;

        var tex = ImageTexture.CreateFromImage(img);
        _cache[key] = tex;
        return tex;
    }

    /// <summary>
    /// HBox containing a glyph + trailing Share-Tech-Mono-style number. Used at every sig/sensor
    /// readout in the System View (§6.1) so callers don't re-plumb the pairing.
    /// </summary>
    public static HBoxContainer CreateLabel(Kind kind, int pixelSize, string text, Color? tintOverride = null)
    {
        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        box.MouseFilter = MouseFilterEnum.Ignore;

        var glyph = new DetectionGlyph(kind, pixelSize, tintOverride);
        box.AddChild(glyph);

        var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, tintOverride ?? DefaultTint(kind));
        box.AddChild(label);

        return box;
    }
}
