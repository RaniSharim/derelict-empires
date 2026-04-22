using System.Collections.Generic;
using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Shared SVG → Texture2D loader with per-size rasterization cache. Uses
/// `Image.LoadSvgFromBuffer` so files don't need the Godot import pipeline.
/// </summary>
public static class SvgIcons
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D? Load(string path, int pixelSize)
    {
        var key = $"{path}@{pixelSize}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        if (!FileAccess.FileExists(path)) return null;
        var bytes = FileAccess.GetFileAsBytes(path);
        if (bytes == null || bytes.Length == 0) return null;

        var image = new Image();
        image.LoadSvgFromBuffer(bytes, pixelSize / 512f);
        if (image.IsEmpty()) return null;

        var tex = ImageTexture.CreateFromImage(image);
        Cache[key] = tex;
        return tex;
    }
}
