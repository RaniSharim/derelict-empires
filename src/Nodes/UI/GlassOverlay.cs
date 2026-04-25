using System.Collections.Generic;
using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Base class for full-screen modal glass overlays (Ship Designer, Tech Tree, Chassis Picker, etc.).
/// Provides: dimmed backdrop, 48px header with breadcrumb + close button, 120ms opacity fade,
/// modal-stack-aware ESC handling. Subclasses populate <see cref="Body"/> with their own content.
/// Z-index is set high so overlays always render above panels and tooltips.
/// </summary>
public partial class GlassOverlay : Control
{
    public const int HeaderHeight = 48;
    public const int OverlayZIndex = 500;
    private const float FadeSeconds = 0.12f;

    /// <summary>Global modal stack. Deeper overlays push onto the top; ESC pops the top only.</summary>
    private static readonly List<GlassOverlay> _stack = new();

    /// <summary>Title shown in the breadcrumb (leftmost segment). Subclass sets in _Ready before AddChild.</summary>
    public string OverlayTitle { get; set; } = "OVERLAY";

    /// <summary>Container subclasses populate with their body content. A VBox so children auto-layout vertically; use SizeFlagsHorizontal=ExpandFill on children that need full width.</summary>
    protected VBoxContainer Body { get; private set; } = null!;

    private Label _breadcrumbLabel = null!;
    private Button _closeButton = null!;
    private ColorRect _backdrop = null!;
    private PanelContainer _contentPanel = null!;
    private bool _isClosing;

    public override void _Ready()
    {
        // CanvasLayer parents have no Rect for anchors to resolve against, so we still
        // size from the viewport explicitly. Anchors are set so children of this Control
        // can use FullRect presets cleanly.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = OverlayZIndex;
        Modulate = new Color(1, 1, 1, 0);

        var vp = GetViewport();
        Size = vp.GetVisibleRect().Size;
        vp.SizeChanged += () =>
        {
            if (IsInsideTree()) Size = GetViewport().GetVisibleRect().Size;
        };

        _backdrop = new ColorRect
        {
            Name = "Backdrop",
            Color = new Color(0, 0, 0, 0.55f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        _backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_backdrop);

        _contentPanel = new PanelContainer { Name = "Content" };
        _contentPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        _contentPanel.OffsetLeft = 48;
        _contentPanel.OffsetTop = 48;
        _contentPanel.OffsetRight = -48;
        _contentPanel.OffsetBottom = -48;
        GlassPanel.Apply(_contentPanel, enableBlur: true);
        AddChild(_contentPanel);

        var layout = new VBoxContainer { Name = "Layout" };
        layout.AddThemeConstantOverride("separation", 0);
        _contentPanel.AddChild(layout);

        layout.AddChild(BuildHeader());

        // Body container — subclass fills this via AddChild; VBox stacks children vertically.
        Body = new VBoxContainer { Name = "Body" };
        Body.SizeFlagsVertical = SizeFlags.ExpandFill;
        Body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Body.AddThemeConstantOverride("separation", 0);
        layout.AddChild(Body);

        _stack.Add(this);
        UpdateBreadcrumb();

        // Fade in
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, FadeSeconds);
    }

    public override void _ExitTree()
    {
        _stack.Remove(this);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Only the topmost overlay reacts to ESC; other keystrokes pass through.
        if (_stack.Count == 0 || _stack[_stack.Count - 1] != this) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo
            && keyEvent.Keycode == Key.Escape)
        {
            RequestClose();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Request to close this overlay. Subclasses can override to intercept (e.g. prompt unsaved changes).
    /// Default: immediate close.
    /// </summary>
    public virtual void RequestClose()
    {
        if (_isClosing) return;
        _isClosing = true;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, FadeSeconds);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private Control BuildHeader()
    {
        var header = new PanelContainer { Name = "Header" };
        header.CustomMinimumSize = new Vector2(0, HeaderHeight);

        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
        };
        headerStyle.SetBorderWidthAll(0);
        headerStyle.BorderWidthBottom = 1;
        headerStyle.BorderColor = UIColors.BorderMid;
        header.AddThemeStyleboxOverride("panel", headerStyle);

        var row = new HBoxContainer { Name = "Row" };
        row.AddThemeConstantOverride("separation", 8);
        header.AddChild(row);

        var pad = new Control { CustomMinimumSize = new Vector2(16, 0) };
        row.AddChild(pad);

        _breadcrumbLabel = new Label { Name = "Breadcrumb", Text = OverlayTitle };
        UIFonts.Style(_breadcrumbLabel, UIFonts.Exo2SemiBold, 14, UIColors.TextBright);
        _breadcrumbLabel.VerticalAlignment = VerticalAlignment.Center;
        _breadcrumbLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(_breadcrumbLabel);

        _closeButton = new Button
        {
            Name = "CloseButton",
            Text = "✕",
            CustomMinimumSize = new Vector2(36, 36),
        };
        UIFonts.StyleButton(_closeButton, UIFonts.Exo2Medium, 14, UIColors.TextLabel);
        GlassPanel.StyleButton(_closeButton);
        _closeButton.Pressed += RequestClose;
        row.AddChild(_closeButton);

        var padR = new Control { CustomMinimumSize = new Vector2(8, 0) };
        row.AddChild(padR);

        return header;
    }

    /// <summary>Rebuild the breadcrumb from the current modal stack (e.g. "TECH TREE ← SHIP DESIGNER").</summary>
    private void UpdateBreadcrumb()
    {
        if (_stack.Count <= 1)
        {
            _breadcrumbLabel.Text = OverlayTitle.ToUpperInvariant();
            return;
        }

        var segments = new List<string>();
        for (int i = _stack.Count - 1; i >= 0; i--)
            segments.Add(_stack[i].OverlayTitle.ToUpperInvariant());

        _breadcrumbLabel.Text = string.Join("  ←  ", segments);
    }
}
