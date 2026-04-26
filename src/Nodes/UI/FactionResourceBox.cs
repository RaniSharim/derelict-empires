using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using System.Collections.Generic;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// One faction's resource display in the topbar. Layout authored in
/// <c>scenes/ui/faction_resource_box.tscn</c>; this script binds nodes,
/// applies faction tints + icons via <see cref="Populate"/>, and pushes
/// live stock/income values into the cells each frame.
/// 2 rows × 3 cells: Row A = common (BasicComponent, SimpleOre, SimpleEnergy),
/// Row B = rare (AdvancedComponent, AdvancedOre, AdvancedEnergy).
/// </summary>
public partial class FactionResourceBox : Control
{
    private PrecursorColor _faction = PrecursorColor.Red;
    private Color _glowColor = Colors.White;
    private Color _bgColor = Colors.Gray;

    private ColorRect _accentBar = null!;
    private ResourceIcon _emblem = null!;
    private readonly PanelContainer[,] _cells = new PanelContainer[2, 3];
    private readonly StyleBoxFlat[,] _cellStyles = new StyleBoxFlat[2, 3];
    private readonly ResourceIcon[,] _icons = new ResourceIcon[2, 3];
    private readonly Label[,] _stockLabels = new Label[2, 3];
    private readonly Label[,] _deltaLabels = new Label[2, 3];

    // Cache last rendered values per cell so _Process skips formatting when nothing changed —
    // 6 cells × 5 boxes × 60fps was allocating ~3,600 strings/sec for static data.
    private readonly float[,] _lastStock = { { float.NaN, float.NaN, float.NaN }, { float.NaN, float.NaN, float.NaN } };
    private readonly float[,] _lastIncome = { { float.NaN, float.NaN, float.NaN }, { float.NaN, float.NaN, float.NaN } };

    /// <summary>Columns are Components, Ore, Energy. Rows are Common, Rare.</summary>
    private static readonly ResourceType[,] ResourceLayout =
    {
        { ResourceType.BasicComponent, ResourceType.SimpleOre, ResourceType.SimpleEnergy },
        { ResourceType.AdvancedComponent, ResourceType.AdvancedOre, ResourceType.AdvancedEnergy }
    };

    private static readonly Dictionary<PrecursorColor, string> FactionEmblemPaths = IconMapping.FactionEmblem;
    private static readonly Dictionary<ResourceType, string> IconPaths = IconMapping.Resource;
    private static readonly Dictionary<string, Texture2D> _iconCache = new();
    private const int ResourceIconSize = 20;
    private const int EmblemIconSize = 28;

    private readonly Dictionary<string, float> _incomeCache = new();

    // Cell unique-names (in scene): row-major matching ResourceLayout.
    private static readonly string[,] CellUniqueNames =
    {
        { "Cell_A0", "Cell_A1", "Cell_A2" },
        { "Cell_B0", "Cell_B1", "Cell_B2" }
    };

    public override void _Ready()
    {
        _accentBar = GetNode<ColorRect>("%AccentBar");
        _emblem = GetNode<ResourceIcon>("%Emblem");

        for (int r = 0; r < 2; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var cell = GetNode<PanelContainer>($"%{CellUniqueNames[r, c]}");
                _cells[r, c] = cell;

                // Duplicate the shared cell stylebox so per-faction BgColor mutations
                // don't bleed across cells (or across boxes sharing the same scene resource).
                var sharedStyle = (StyleBoxFlat)cell.GetThemeStylebox("panel");
                var ownStyle = (StyleBoxFlat)sharedStyle.Duplicate();
                cell.AddThemeStyleboxOverride("panel", ownStyle);
                _cellStyles[r, c] = ownStyle;

                _icons[r, c] = cell.GetNode<ResourceIcon>("VBox/Icon");
                _stockLabels[r, c] = cell.GetNode<Label>("VBox/Stock");
                _deltaLabels[r, c] = cell.GetNode<Label>("VBox/Delta");

                UIFonts.Style(_stockLabels[r, c], UIFonts.Main, UIFonts.NormalSize, Colors.White, shadow: true);
                UIFonts.Style(_deltaLabels[r, c], UIFonts.Main, UIFonts.SmallSize, UIColors.DeltaPosBright);
            }
        }

        ApplyFactionVisuals();
    }

    /// <summary>Bind this box to a faction. Safe to call before or after _Ready.</summary>
    public void Populate(PrecursorColor faction)
    {
        _faction = faction;
        if (IsNodeReady())
            ApplyFactionVisuals();
    }

    private void ApplyFactionVisuals()
    {
        _glowColor = UIColors.GetFactionGlow(_faction);
        _bgColor = UIColors.GetFactionBg(_faction);

        _accentBar.Color = new Color(_glowColor.R * 0.7f, _glowColor.G * 0.7f, _glowColor.B * 0.7f, 0.85f);

        if (FactionEmblemPaths.TryGetValue(_faction, out var emblemPath))
        {
            _emblem.Texture = LoadIconFromPath(emblemPath, EmblemIconSize);
            _emblem.Tint = new Color(1f, 1f, 1f, 0.9f);
        }

        var cellBg = new Color(_bgColor.R * 0.6f, _bgColor.G * 0.6f, _bgColor.B * 0.6f, 0.55f);
        var iconTint = new Color(
            Mathf.Min(_glowColor.R * 1.4f, 1f),
            Mathf.Min(_glowColor.G * 1.4f, 1f),
            Mathf.Min(_glowColor.B * 1.4f, 1f),
            0.8f);

        for (int r = 0; r < 2; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                _cellStyles[r, c].BgColor = cellBg;
                _icons[r, c].Texture = LoadIcon(ResourceLayout[r, c]);
                _icons[r, c].Tint = iconTint;
                // Force re-render on next _Process tick (faction changed → stock/income context reset)
                _lastStock[r, c] = float.NaN;
                _lastIncome[r, c] = float.NaN;
            }
        }
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
                if (amount != _lastStock[row, col])
                {
                    _stockLabels[row, col].Text = FormatStock(amount);
                    _lastStock[row, col] = amount;
                }

                var key = EmpireData.ResourceKey(_faction, type);
                float income = _incomeCache.GetValueOrDefault(key);
                if (income != _lastIncome[row, col])
                {
                    _deltaLabels[row, col].Text = FormatDelta(income);
                    _deltaLabels[row, col].AddThemeColorOverride("font_color",
                        income >= 0 ? UIColors.DeltaPosBright : UIColors.DeltaNegBright);
                    _lastIncome[row, col] = income;
                }
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

    private static Texture2D? LoadIcon(ResourceType type)
    {
        if (!IconPaths.TryGetValue(type, out var path)) return null;
        return LoadIconFromPath(path, ResourceIconSize);
    }

    /// <summary>Load and rasterize an SVG at a given pixel size. Cached per (path, size).</summary>
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
