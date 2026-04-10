using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Events;

public enum EventType { SpaceStorm, RogueAI, Pirates, PrecursorDefenseActivation, AnomalyDiscovered }

public class GameEvent
{
    public int Id { get; set; }
    public EventType Type { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int SystemId { get; set; } = -1;
    public int AffectedEmpireId { get; set; } = -1;
    public int Tick { get; set; }
}

/// <summary>
/// Generates random events during gameplay. Pure C#, seeded.
/// </summary>
public class RandomEventSystem
{
    private readonly List<GameEvent> _history = new();
    private int _nextEventId;

    public event Action<GameEvent>? EventTriggered;

    public IReadOnlyList<GameEvent> History => _history;

    /// <summary>
    /// Roll for random events each slow tick. Low probability per tick.
    /// </summary>
    public void ProcessTick(int currentTick, int systemCount, GameRandom rng)
    {
        // ~2% chance per tick of a random event
        if (!rng.Chance(0.02f)) return;

        var type = (EventType)rng.RangeInt(Enum.GetValues<EventType>().Length);
        int systemId = rng.RangeInt(systemCount);

        var evt = new GameEvent
        {
            Id = _nextEventId++,
            Type = type,
            SystemId = systemId,
            Tick = currentTick,
            Title = GetTitle(type),
            Description = GetDescription(type)
        };

        _history.Add(evt);
        EventTriggered?.Invoke(evt);
    }

    private static string GetTitle(EventType type) => type switch
    {
        EventType.SpaceStorm => "Ion Storm Detected",
        EventType.RogueAI => "Rogue AI Activation",
        EventType.Pirates => "Pirate Raid",
        EventType.PrecursorDefenseActivation => "Precursor Defense Online",
        EventType.AnomalyDiscovered => "Spatial Anomaly",
        _ => "Unknown Event"
    };

    private static string GetDescription(EventType type) => type switch
    {
        EventType.SpaceStorm => "A violent ion storm disrupts sensors and shields in the area.",
        EventType.RogueAI => "An automated precursor defense system has reactivated.",
        EventType.Pirates => "A pirate band has been spotted raiding nearby systems.",
        EventType.PrecursorDefenseActivation => "Ancient weapons have come online near a precursor site.",
        EventType.AnomalyDiscovered => "Scanners detect an unusual spatial phenomenon.",
        _ => ""
    };
}
