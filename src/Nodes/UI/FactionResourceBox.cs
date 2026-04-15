using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using System.Collections.Generic;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Per-resource stock and gain values.
/// </summary>
public record struct ResourceStockAndGain(float Stock, float Gain);

/// <summary>
/// All 6 resource stocks and gains for one faction.
/// Fields: PartsBasic, PartsAdvanced, MaterialsBasic, MaterialsAdvanced, EnergyBasic, EnergyAdvanced.
/// </summary>
public class FactionResourceData
{
    public ResourceStockAndGain PartsBasic;
    public ResourceStockAndGain PartsAdvanced;
    public ResourceStockAndGain MaterialsBasic;
    public ResourceStockAndGain MaterialsAdvanced;
    public ResourceStockAndGain EnergyBasic;
    public ResourceStockAndGain EnergyAdvanced;

    public ResourceStockAndGain Get(ResourceType type) => type switch
    {
        ResourceType.SimpleParts => PartsBasic,
        ResourceType.AdvancedParts => PartsAdvanced,
        ResourceType.SimpleMaterials => MaterialsBasic,
        ResourceType.AdvancedMaterials => MaterialsAdvanced,
        ResourceType.SimpleEnergy => EnergyBasic,
        ResourceType.AdvancedEnergy => EnergyAdvanced,
        _ => default
    };

    public void Set(ResourceType type, ResourceStockAndGain value)
    {
        switch (type)
        {
            case ResourceType.SimpleParts: PartsBasic = value; break;
            case ResourceType.AdvancedParts: PartsAdvanced = value; break;
            case ResourceType.SimpleMaterials: MaterialsBasic = value; break;
            case ResourceType.AdvancedMaterials: MaterialsAdvanced = value; break;
            case ResourceType.SimpleEnergy: EnergyBasic = value; break;
            case ResourceType.AdvancedEnergy: EnergyAdvanced = value; break;
        }
    }
}

/// <summary>
/// One faction's resource display in the topbar.
/// Shows 6 resources: 3 common (Row A) + 3 rare (Row B), each with stock on top + gain below.
/// Uses Control base (not PanelContainer) to avoid auto-sizing.
/// </summary>
public partial class FactionResourceBox : Control
{
    private readonly PrecursorColor _faction;
    private readonly Color _glowColor;
    private readonly Color _bgColor;

    private readonly Label[,] _stockLabels = new Label[2, 3];
    private readonly Label[,] _deltaLabels = new Label[2, 3];

    private static readonly ResourceType[,] ResourceLayout =
    {
        { ResourceType.SimpleParts, ResourceType.SimpleMaterials, ResourceType.SimpleEnergy },
        { ResourceType.AdvancedParts, ResourceType.AdvancedMaterials, ResourceType.AdvancedEnergy }
    };

    /// <summary>Per-faction resource data. Access by color name (e.g. "Red", "Blue").</summary>
    public static readonly Dictionary<string, FactionResourceData> FactionData = new()
    {
        ["Red"] = new FactionResourceData
        {
            PartsBasic = new(45230, 340),
            PartsAdvanced = new(12500, -180),
            MaterialsBasic = new(67800, 520),
            MaterialsAdvanced = new(18900, 210),
            EnergyBasic = new(89100, 760),
            EnergyAdvanced = new(10200, -90),
        },
        ["Blue"] = new FactionResourceData
        {
            PartsBasic = new(32100, 510),
            PartsAdvanced = new(21800, 130),
            MaterialsBasic = new(54600, -320),
            MaterialsAdvanced = new(15700, 180),
            EnergyBasic = new(71200, 440),
            EnergyAdvanced = new(23400, -250),
        },
        ["Green"] = new FactionResourceData
        {
            PartsBasic = new(58700, 280),
            PartsAdvanced = new(16300, -120),
            MaterialsBasic = new(82400, 680),
            MaterialsAdvanced = new(28100, 340),
            EnergyBasic = new(43900, 190),
            EnergyAdvanced = new(11800, -60),
        },
        ["Gold"] = new FactionResourceData
        {
            PartsBasic = new(21400, 720),
            PartsAdvanced = new(34600, 260),
            MaterialsBasic = new(47300, -150),
            MaterialsAdvanced = new(19800, 430),
            EnergyBasic = new(65100, 550),
            EnergyAdvanced = new(27900, -310),
        },
        ["Purple"] = new FactionResourceData
        {
            PartsBasic = new(73500, 410),
            PartsAdvanced = new(28400, -200),
            MaterialsBasic = new(39200, 610),
            MaterialsAdvanced = new(22700, 170),
            EnergyBasic = new(56800, 380),
            EnergyAdvanced = new(14100, -140),
        },
    };

    // Faction emblem icon paths (from IconMapping — white on transparent SVGs)
    private static readonly Dictionary<PrecursorColor, string> FactionEmblemPaths = IconMapping.FactionEmblem;

    // Resource icon paths (from IconMapping — white on transparent SVGs)
    private static readonly Dictionary<ResourceType, string> IconPaths = IconMapping.Resource;
    private static readonly Dictionary<string, Texture2D> _iconCache = new();

    private readonly Dictionary<string, float> _incomeCache = new();

    public FactionResourceBox(PrecursorColor faction)
    {
        _faction = faction;
        _glowColor = UIColors.GetFactionGlow(faction);
        _bgColor = UIColors.GetFactionBg(faction);
        CustomMinimumSize = new Vector2(150, 0); // floor for small screens
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsStretchRatio = 0.4f; // narrower — keep icons close to text
        SizeFlagsVertical = SizeFlags.ExpandFill;
        ClipContents = true;
    }

    public override void _Ready()
    {
        // Outer background = DARK (visible as separator lines between bright cells)
        var bg = new PanelContainer { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(4 / 255f, 6 / 255f, 14 / 255f, 0.95f); // very dark
        bgStyle.SetBorderWidthAll(1);
        bgStyle.BorderColor = UIColors.BorderDim;
        bgStyle.SetCornerRadiusAll(4);
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        AddChild(bg);

        // Solid accent bar on the left — 38px wide, holds faction emblem
        var accentBar = new ColorRect();
        accentBar.Color = new Color(_glowColor.R * 0.7f, _glowColor.G * 0.7f, _glowColor.B * 0.7f, 0.85f);
        accentBar.CustomMinimumSize = new Vector2(38, 0);
        accentBar.SetAnchorsPreset(LayoutPreset.LeftWide);
        AddChild(accentBar);

        // Faction emblem icon centered in accent bar (28px, white)
        if (FactionEmblemPaths.TryGetValue(_faction, out var emblemPath))
        {
            var emblemTex = LoadIconFromPath(emblemPath, 28);
            if (emblemTex != null)
            {
                var emblem = new ResourceIcon(emblemTex, new Color(1f, 1f, 1f, 0.9f), _faction);
                emblem.CustomMinimumSize = new Vector2(28, 28);
                emblem.SetAnchorsPreset(LayoutPreset.Center);
                emblem.OffsetLeft = -14;
                emblem.OffsetRight = 14;
                emblem.OffsetTop = -14;
                emblem.OffsetBottom = 14;
                emblem.MouseFilter = MouseFilterEnum.Ignore;
                accentBar.AddChild(emblem);
            }
        }

        // Content VBox fills the control — spacing = dark separator width
        var content = new VBoxContainer { Name = "Content" };
        content.SetAnchorsPreset(LayoutPreset.FullRect);
        content.OffsetLeft = 38; // after wider accent bar
        content.AddThemeConstantOverride("separation", 2); // minimal gap between rows
        AddChild(content);

        // Row A — Common resources (fills top half)
        AddResourceRow(content, 0, false);

        // Row B — Rare resources (fills bottom half)
        AddResourceRow(content, 1, true);

        // Initialize with dummy data
        ApplyDummyData();
    }

    private void AddResourceRow(VBoxContainer parent, int rowIndex, bool isRare)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 2); // tight gap between cells
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.SizeFlagsVertical = SizeFlags.ExpandFill;
        parent.AddChild(row);

        for (int col = 0; col < 3; col++)
        {
            var cellContainer = new PanelContainer();
            cellContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            cellContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
            var cellStyle = new StyleBoxFlat();
            cellStyle.BgColor = new Color(_bgColor.R * 0.6f, _bgColor.G * 0.6f, _bgColor.B * 0.6f, 0.55f);
            cellStyle.SetBorderWidthAll(0);
            cellStyle.SetCornerRadiusAll(0);
            cellStyle.ContentMarginLeft = 1;
            cellStyle.ContentMarginRight = 1;
            cellStyle.ContentMarginTop = 0;
            cellStyle.ContentMarginBottom = 0;
            cellContainer.AddThemeStyleboxOverride("panel", cellStyle);
            row.AddChild(cellContainer);

            // VBox fills the entire cell; children centered horizontally via ExpandFill + Center alignment
            var cellVBox = new VBoxContainer();
            cellVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            cellVBox.SizeFlagsVertical = SizeFlags.ExpandFill;
            cellVBox.Alignment = BoxContainer.AlignmentMode.Center;
            cellVBox.AddThemeConstantOverride("separation", 0);
            cellContainer.AddChild(cellVBox);

            // Resource icon — 20px, centered, tinted with faction glow
            var resType = ResourceLayout[rowIndex, col];
            var iconTex = LoadIcon(resType);
            var lighterTint = new Color(
                Mathf.Min(_glowColor.R * 1.4f, 1f),
                Mathf.Min(_glowColor.G * 1.4f, 1f),
                Mathf.Min(_glowColor.B * 1.4f, 1f),
                0.8f);
            var icon = new ResourceIcon(iconTex, lighterTint, _faction);
            icon.CustomMinimumSize = new Vector2(20, 20);
            icon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            cellVBox.AddChild(icon);

            // Stock label — full width, text centered
            var stockLabel = new Label { Text = "0" };
            UIFonts.Style(stockLabel, UIFonts.ShareTechMono, 16, Colors.White, shadow: true);
            stockLabel.HorizontalAlignment = HorizontalAlignment.Center;
            stockLabel.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
            cellVBox.AddChild(stockLabel);
            _stockLabels[rowIndex, col] = stockLabel;

            // Delta label — full width, text centered
            var deltaLabel = new Label { Text = "(+0)" };
            UIFonts.Style(deltaLabel, UIFonts.ShareTechMono, 12, UIColors.DeltaPosBright, shadow: true);
            deltaLabel.HorizontalAlignment = HorizontalAlignment.Center;
            deltaLabel.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
            cellVBox.AddChild(deltaLabel);
            _deltaLabels[rowIndex, col] = deltaLabel;
        }
    }

    private void ApplyDummyData()
    {
        var key = _faction.ToString();
        if (!FactionData.TryGetValue(key, out var data)) return;

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                var type = ResourceLayout[row, col];
                var sg = data.Get(type);
                _stockLabels[row, col].Text = FormatStock(sg.Stock);
                _deltaLabels[row, col].Text = FormatDelta(sg.Gain);
                _deltaLabels[row, col].AddThemeColorOverride("font_color",
                    sg.Gain >= 0 ? UIColors.DeltaPosBright : UIColors.DeltaNegBright);
            }
        }
    }

    private static void AddDivider(VBoxContainer parent, Color color)
    {
        var div = new ColorRect();
        div.CustomMinimumSize = new Vector2(0, 1);
        div.Color = color;
        parent.AddChild(div);
    }

    public override void _Process(double delta)
    {
        var empire = GameManager.Instance?.LocalPlayerEmpire;
        if (empire == null) return;

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                var type = ResourceLayout[row, col];
                float amount = empire.GetResource(_faction, type);
                // Don't overwrite dummy data with zeros
                if (amount < 0.01f && _incomeCache.Count == 0) continue;
                _stockLabels[row, col].Text = FormatStock(amount);

                var key = EmpireData.ResourceKey(_faction, type);
                float income = _incomeCache.GetValueOrDefault(key);
                _deltaLabels[row, col].Text = FormatDelta(income);
                _deltaLabels[row, col].AddThemeColorOverride("font_color",
                    income >= 0 ? UIColors.DeltaPosBright : UIColors.DeltaNegBright);
            }
        }
    }

    public void UpdateIncome(Dictionary<string, float> income)
    {
        _incomeCache.Clear();
        foreach (var kv in income)
        {
            if (kv.Key.StartsWith(_faction.ToString()))
                _incomeCache[kv.Key] = kv.Value;
        }
    }

    private static string FormatStock(float amount)
    {
        if (amount >= 1_000_000) return $"{amount / 1_000_000f:F1}M";
        if (amount >= 1_000) return $"{amount / 1_000f:F1}K";
        return $"{amount:F0}";
    }

    private static string FormatDelta(float income)
    {
        if (income > 0.01f) return $"(+{income:F0})";
        if (income < -0.01f) return $"({income:F0})";
        return "(+0)";
    }

    private const int ResourceIconSize = 20; // rasterize resource SVGs at this size

    private static Texture2D? LoadIcon(ResourceType type)
    {
        if (!IconPaths.TryGetValue(type, out var path)) return null;
        return LoadIconFromPath(path, ResourceIconSize);
    }

    /// <summary>Load and rasterize an SVG at a given pixel size. Shared by resource + emblem loaders.</summary>
    private static Texture2D? LoadIconFromPath(string path, int pixelSize)
    {
        var cacheKey = $"{path}@{pixelSize}";
        if (_iconCache.TryGetValue(cacheKey, out var cached)) return cached;

        if (!FileAccess.FileExists(path)) return null;

        var svgBytes = FileAccess.GetFileAsBytes(path);
        if (svgBytes == null || svgBytes.Length == 0) return null;
        var image = new Image();
        image.LoadSvgFromBuffer(svgBytes, pixelSize / 512f); // 512 = native SVG viewBox
        if (image.IsEmpty()) return null;
        var tex = ImageTexture.CreateFromImage(image);
        _iconCache[cacheKey] = tex;
        return tex;
    }
}

/// <summary>Draws a resource SVG icon at fixed size, tinted with faction color. Falls back to FactionIcon shape.</summary>
public partial class ResourceIcon : Control
{
    private readonly Texture2D? _texture;
    private readonly Color _tint;
    private readonly PrecursorColor _faction;

    public ResourceIcon(Texture2D? texture, Color tint, PrecursorColor faction)
    {
        _texture = texture;
        _tint = tint;
        _faction = faction;
    }

    public override void _Draw()
    {
        if (_texture != null)
        {
            DrawTextureRect(_texture, new Rect2(Vector2.Zero, Size), false, _tint);
        }
        else
        {
            // Fallback: draw faction shape
            FactionIcon.DrawShape(this, _faction, _tint);
        }
    }
}

public partial class FactionIcon : Control
{
    private readonly PrecursorColor _faction;
    private readonly Color _color;

    public FactionIcon(PrecursorColor faction, Color color)
    {
        _faction = faction;
        _color = color;
    }

    public static void DrawShape(Control target, PrecursorColor faction, Color color)
    {
        float w = target.Size.X;
        float h = target.Size.Y;
        var center = new Vector2(w / 2, h / 2);
        switch (faction)
        {
            case PrecursorColor.Red:
                var hex2 = new Vector2[6];
                for (int j = 0; j < 6; j++)
                {
                    float a = Mathf.Pi / 3 * j + Mathf.Pi / 6;
                    hex2[j] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (w / 2);
                }
                target.DrawPolygon(hex2, new[] { color });
                break;
            case PrecursorColor.Blue:
                target.DrawPolygon(new[] { new Vector2(w/2,0), new Vector2(w,h/2), new Vector2(w/2,h), new Vector2(0,h/2) }, new[] { color });
                break;
            case PrecursorColor.Green:
                target.DrawPolygon(new[] { new Vector2(w/2,0), new Vector2(w,h), new Vector2(0,h) }, new[] { color });
                break;
            case PrecursorColor.Gold:
                target.DrawRect(new Rect2(0, 0, w, h), color);
                break;
            default:
                target.DrawCircle(center, w / 2, color);
                break;
        }
    }

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;
        var center = new Vector2(w / 2, h / 2);

        switch (_faction)
        {
            case PrecursorColor.Red: // Hexagon
                var hex = new Vector2[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle = Mathf.Pi / 3 * i + Mathf.Pi / 6;
                    hex[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (w / 2);
                }
                DrawPolygon(hex, new[] { _color });
                break;
            case PrecursorColor.Blue: // Diamond
                DrawPolygon(new[] {
                    new Vector2(w / 2, 0),
                    new Vector2(w, h / 2),
                    new Vector2(w / 2, h),
                    new Vector2(0, h / 2)
                }, new[] { _color });
                break;
            case PrecursorColor.Green: // Triangle
                DrawPolygon(new[] {
                    new Vector2(w / 2, 0),
                    new Vector2(w, h),
                    new Vector2(0, h)
                }, new[] { _color });
                break;
            case PrecursorColor.Gold: // Square
                DrawRect(new Rect2(0, 0, w, h), _color);
                break;
            case PrecursorColor.Purple: // Star
                var star = new Vector2[10];
                float outR = w / 2;
                float inR = w / 4;
                for (int i = 0; i < 10; i++)
                {
                    float angle = Mathf.Pi / 5 * i - Mathf.Pi / 2;
                    float r = (i % 2 == 0) ? outR : inR;
                    star[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                }
                DrawPolygon(star, new[] { _color });
                break;
            default:
                DrawRect(new Rect2(0, 0, w, h), _color);
                break;
        }
    }
}
