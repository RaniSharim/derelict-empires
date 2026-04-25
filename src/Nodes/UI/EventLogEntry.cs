using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Single row in the event log: colored category dot + body text.
/// Layout in <c>scenes/ui/event_log_entry.tscn</c>; populated by <see cref="EventLog"/>
/// via <see cref="Populate"/>.
/// </summary>
public partial class EventLogEntry : MarginContainer
{
    [Export] private EventDot _dot = null!;
    [Export] private Label _text = null!;

    public override void _Ready()
    {
        UIFonts.Style(_text, UIFonts.Main, UIFonts.NormalSize, UIColors.TextBody);
    }

    public void Populate(string text, EventCategory category)
    {
        _text.Text = text;
        _dot.DotColor = GetCategoryColor(category);
    }

    private static Color GetCategoryColor(EventCategory category) => category switch
    {
        EventCategory.Combat   => UIColors.Alert,
        EventCategory.Movement => UIColors.Moving,
        EventCategory.Research => new Color("#b366e8"),
        EventCategory.Build    => UIColors.Accent,
        EventCategory.Info     => UIColors.TextDim,
        _                      => UIColors.TextDim,
    };
}
