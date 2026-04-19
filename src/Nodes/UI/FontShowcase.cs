using Godot;

namespace DerlictEmpires.Nodes.UI;

public partial class FontShowcase : Control
{
    private static readonly int[] Sizes = { 8, 10, 12, 14, 16 };

    // Config matrix is rendered against these two fonts at a mid-critical size.
    private const string RajdhaniPath = "res://assets/fonts/Rajdhani-Medium.ttf";
    private const string MonoPath     = "res://assets/fonts/IBMPlexMono-Regular.ttf";

    private record FontEntry(string Name, Font? Font);

    public override void _Ready()
    {
        AnchorRight = 1f;
        AnchorBottom = 1f;
        MouseFilter = MouseFilterEnum.Stop;

        var bg = new ColorRect
        {
            Color = new Color(0.02f, 0.03f, 0.05f, 0.98f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
        };
        AddChild(bg);

        var scroll = new ScrollContainer
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 32,
            OffsetRight = -32,
            OffsetTop = 24,
            OffsetBottom = -24,
        };
        AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(vbox);

        var header = new Label { Text = "FONT SHOWCASE — press F11 or Esc to close" };
        UIFonts.Style(header, UIFonts.Exo2SemiBold, 18, UIColors.TextBright);
        vbox.AddChild(header);

        SectionGap(vbox, 12);

        BuildFontCatalogue(vbox);
        SectionGap(vbox, 24);
        BuildConfigMatrix(vbox, "RAJDHANI MEDIUM", RajdhaniPath, size: 12);
        SectionGap(vbox, 16);
        BuildConfigMatrix(vbox, "IBM PLEX MONO", MonoPath, size: 12);
        SectionGap(vbox, 24);
        BuildImportVsRuntime(vbox);
        SectionGap(vbox, 24);
        BuildPixelSnapDemo(vbox);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo
            && (k.Keycode == Key.Escape || k.Keycode == Key.F11))
        {
            // Walk up — our parent is a CanvasLayer created just for us.
            var parent = GetParent();
            (parent ?? this).QueueFree();
            GetViewport().SetInputAsHandled();
        }
    }

    // ───── Section 1: full font catalogue at 5 sizes ─────
    private static void BuildFontCatalogue(VBoxContainer vbox)
    {
        SectionHeader(vbox, "FONT CATALOGUE");

        var firaMono        = LoadConfigured("res://assets/fonts/FiraMono-Regular.ttf");
        var firaMonoMedium  = LoadConfigured("res://assets/fonts/FiraMono-Medium.ttf");
        var b612Mono        = LoadConfigured("res://assets/fonts/B612Mono-Regular.ttf");
        var b612MonoBold    = LoadConfigured("res://assets/fonts/B612Mono-Bold.ttf");
        var oxaniumMedium   = LoadConfiguredVariation("res://assets/fonts/Oxanium-Variable.ttf", 500);
        var oxaniumSemiBold = LoadConfiguredVariation("res://assets/fonts/Oxanium-Variable.ttf", 600);
        var oxaniumBold     = LoadConfiguredVariation("res://assets/fonts/Oxanium-Variable.ttf", 700);

        var fonts = new FontEntry[]
        {
            new("Exo2Bold",              UIFonts.Exo2Bold),
            new("Exo2SemiBold",          UIFonts.Exo2SemiBold),
            new("Exo2Medium",            UIFonts.Exo2Medium),
            new("RajdhaniSemiBold",      UIFonts.RajdhaniSemiBold),
            new("RajdhaniMedium",        UIFonts.RajdhaniMedium),
            new("RajdhaniMediumTracked", UIFonts.RajdhaniMediumTracked),
            new("RajdhaniRegular",       UIFonts.RajdhaniRegular),
            new("Mono",                  UIFonts.Mono),
            new("MonoMedium",            UIFonts.MonoMedium),
            new("MonoTracked",           UIFonts.MonoTracked),
            new("MonoMediumTracked",     UIFonts.MonoMediumTracked),
            new("FiraMono",              firaMono),
            new("FiraMonoMedium",        firaMonoMedium),
            new("B612Mono",              b612Mono),
            new("B612MonoBold",          b612MonoBold),
            new("OxaniumMedium",         oxaniumMedium),
            new("OxaniumSemiBold",       oxaniumSemiBold),
            new("OxaniumBold",           oxaniumBold),
        };

        foreach (var entry in fonts)
        {
            vbox.AddChild(new HSeparator());
            foreach (var size in Sizes)
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 16);
                vbox.AddChild(row);

                var tag = new Label { Text = $"{size,2}px" };
                UIFonts.Style(tag, UIFonts.MonoMedium, 11, UIColors.TextDim);
                tag.CustomMinimumSize = new Vector2(52, 0);
                row.AddChild(tag);

                var sample = new Label { Text = $"{entry.Name} 0123456789" };
                UIFonts.Style(sample, entry.Font, size, UIColors.TextBright);
                row.AddChild(sample);
            }
        }
    }

    // ───── Section 2: FontFile config matrix ─────
    private static void BuildConfigMatrix(VBoxContainer vbox, string title, string path, int size)
    {
        SectionHeader(vbox, $"{title} @ {size}px — CONFIG AXES");

        AddAxis(vbox, path, size, "Hinting",
            ("None",               LoadConfigured(path, hinting: TextServer.Hinting.None)),
            ("Light",              LoadConfigured(path, hinting: TextServer.Hinting.Light)),
            ("Normal (current)",   LoadConfigured(path, hinting: TextServer.Hinting.Normal)));

        AddAxis(vbox, path, size, "SubpixelPositioning",
            ("Disabled (current)", LoadConfigured(path, subpixel: TextServer.SubpixelPositioning.Disabled)),
            ("Auto",               LoadConfigured(path, subpixel: TextServer.SubpixelPositioning.Auto)),
            ("OneHalf",            LoadConfigured(path, subpixel: TextServer.SubpixelPositioning.OneHalf)),
            ("OneQuarter",         LoadConfigured(path, subpixel: TextServer.SubpixelPositioning.OneQuarter)));

        AddAxis(vbox, path, size, "Antialiasing",
            ("None",               LoadConfigured(path, aa: TextServer.FontAntialiasing.None)),
            ("Gray (current)",     LoadConfigured(path, aa: TextServer.FontAntialiasing.Gray)),
            ("LCD",                LoadConfigured(path, aa: TextServer.FontAntialiasing.Lcd)));

        AddAxis(vbox, path, size, "ForceAutohinter",
            ("Off",                LoadConfigured(path, forceAutohinter: false)),
            ("On (current)",       LoadConfigured(path, forceAutohinter: true)));

        AddAxis(vbox, path, size, "MultichannelSDF",
            ("Off (current)",      LoadConfigured(path, msdf: false)),
            ("On",                 LoadConfigured(path, msdf: true)));

        AddAxis(vbox, path, size, "GenerateMipmaps",
            ("Off (current)",      LoadConfigured(path, mipmaps: false)),
            ("On",                 LoadConfigured(path, mipmaps: true)));
    }

    // ───── Section 3: GD.Load (uses .ttf.import) vs runtime FileAccess ─────
    private static void BuildImportVsRuntime(VBoxContainer vbox)
    {
        SectionHeader(vbox, "IMPORT PIPELINE vs RUNTIME LOAD");

        var importedRajdhani = GD.Load<Font>(RajdhaniPath);
        var importedMono     = GD.Load<Font>(MonoPath);
        var runtimeRajdhani  = LoadConfigured(RajdhaniPath);
        var runtimeMono      = LoadConfigured(MonoPath);

        AddCompareRow(vbox, "Rajdhani 12px GD.Load (uses .ttf.import)",     importedRajdhani, 12);
        AddCompareRow(vbox, "Rajdhani 12px FileAccess (current in UIFonts)", runtimeRajdhani, 12);
        SectionGap(vbox, 6);
        AddCompareRow(vbox, "Mono 12px GD.Load (uses .ttf.import)",         importedMono, 12);
        AddCompareRow(vbox, "Mono 12px FileAccess (current in UIFonts)",    runtimeMono, 12);
    }

    // ───── Section 4: pixel snap — same label at integer vs fractional X ─────
    private static void BuildPixelSnapDemo(VBoxContainer vbox)
    {
        SectionHeader(vbox, "PIXEL POSITION — integer vs fractional X");

        var font = LoadConfigured(RajdhaniPath);
        var offsets = new (string name, float x)[]
        {
            ("x = 0.00 (integer)", 0f),
            ("x = 0.25",           0.25f),
            ("x = 0.50",           0.50f),
            ("x = 0.75",           0.75f),
            ("x = 1.00 (integer)", 1.00f),
        };

        foreach (var (name, dx) in offsets)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 16);
            vbox.AddChild(row);

            var tag = new Label { Text = name };
            UIFonts.Style(tag, UIFonts.MonoMedium, 11, UIColors.TextDim);
            tag.CustomMinimumSize = new Vector2(200, 0);
            row.AddChild(tag);

            // Wrapping container so we can offset the inner label manually.
            var slot = new Control { CustomMinimumSize = new Vector2(600, 18) };
            row.AddChild(slot);

            var sample = new Label { Text = "Rajdhani 0123456789 abcABC" };
            UIFonts.Style(sample, font, 12, UIColors.TextBright);
            sample.Position = new Vector2(dx, 0);
            slot.AddChild(sample);
        }
    }

    // ───── helpers ─────
    private static void SectionHeader(VBoxContainer vbox, string title)
    {
        var h = new Label { Text = title };
        UIFonts.Style(h, UIFonts.Exo2SemiBold, 14, UIColors.TextLabel);
        vbox.AddChild(h);
    }

    private static void SectionGap(VBoxContainer vbox, int px)
    {
        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, px) });
    }

    private static void AddAxis(VBoxContainer vbox, string path, int size, string axisName,
        params (string label, FontFile? font)[] values)
    {
        var axisLbl = new Label { Text = axisName };
        UIFonts.Style(axisLbl, UIFonts.RajdhaniMediumTracked, 11, UIColors.TextDim);
        vbox.AddChild(axisLbl);

        var shortName = System.IO.Path.GetFileNameWithoutExtension(path);
        foreach (var (name, font) in values)
        {
            AddCompareRow(vbox, $"  {axisName} = {name}", font, size, shortName);
        }
        SectionGap(vbox, 8);
    }

    private static void AddCompareRow(VBoxContainer vbox, string tagText, Font? font, int size, string sampleName = null!)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(row);

        var tag = new Label { Text = tagText };
        UIFonts.Style(tag, UIFonts.MonoMedium, 11, UIColors.TextDim);
        tag.CustomMinimumSize = new Vector2(360, 0);
        row.AddChild(tag);

        var body = sampleName ?? "Sample";
        var sample = new Label { Text = $"{body} 0123456789 abcABC" };
        UIFonts.Style(sample, font, size, UIColors.TextBright);
        row.AddChild(sample);
    }

    private static FontFile? LoadConfigured(string path,
        TextServer.Hinting hinting = TextServer.Hinting.Normal,
        bool forceAutohinter = true,
        TextServer.FontAntialiasing aa = TextServer.FontAntialiasing.Gray,
        TextServer.SubpixelPositioning subpixel = TextServer.SubpixelPositioning.Disabled,
        bool msdf = false,
        bool mipmaps = false)
    {
        if (!FileAccess.FileExists(path)) return null;
        var data = FileAccess.GetFileAsBytes(path);
        if (data == null || data.Length == 0) return null;

        var font = new FontFile { Data = data };
        font.Hinting = hinting;
        font.ForceAutohinter = forceAutohinter;
        font.Antialiasing = aa;
        font.SubpixelPositioning = subpixel;
        font.MultichannelSignedDistanceField = msdf;
        font.GenerateMipmaps = mipmaps;
        return font;
    }

    private static Font? LoadConfiguredVariation(string path, int weight)
    {
        var baseFont = LoadConfigured(path);
        if (baseFont == null) return null;
        var v = new FontVariation { BaseFont = baseFont };
        v.VariationOpentype = new Godot.Collections.Dictionary { { "wght", weight } };
        return v;
    }
}
