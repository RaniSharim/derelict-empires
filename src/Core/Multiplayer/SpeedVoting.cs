using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Multiplayer;

/// <summary>
/// Speed voting for multiplayer. Game runs at averaged vote among connected players.
/// Pure C#.
/// </summary>
public class SpeedVoting
{
    private readonly Dictionary<int, GameSpeed> _votes = new();

    public void SetVote(int playerId, GameSpeed speed) =>
        _votes[playerId] = speed;

    public void RemovePlayer(int playerId) =>
        _votes.Remove(playerId);

    /// <summary>Get the game speed as the average of all player votes.</summary>
    public GameSpeed GetResolvedSpeed()
    {
        if (_votes.Count == 0) return GameSpeed.Normal;

        float avg = (float)_votes.Values.Average(v => (int)v);

        // Round to nearest valid speed
        if (avg <= 0.5f) return GameSpeed.Paused;
        if (avg <= 1.5f) return GameSpeed.Normal;
        if (avg <= 3f) return GameSpeed.Fast;
        if (avg <= 6f) return GameSpeed.Faster;
        return GameSpeed.Fastest;
    }
}
