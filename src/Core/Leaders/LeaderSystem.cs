using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Leaders;

public enum LeaderType { Admiral, Governor }

public class LeaderTrait
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public LeaderType AppliesTo { get; set; }
    public Dictionary<string, float> Modifiers { get; set; } = new();

    public static readonly LeaderTrait[] AdmiralTraits = new[]
    {
        new LeaderTrait { Id = "aggressive", Name = "Aggressive Tactician", AppliesTo = LeaderType.Admiral, Modifiers = { ["damage"] = 0.15f, ["survivability"] = -0.05f } },
        new LeaderTrait { Id = "carrier_spec", Name = "Carrier Specialist", AppliesTo = LeaderType.Admiral, Modifiers = { ["carrier_slots"] = 1f, ["fighter_eff"] = 0.10f } },
        new LeaderTrait { Id = "logistics_master", Name = "Logistics Master", AppliesTo = LeaderType.Admiral, Modifiers = { ["supply_consumption"] = -0.20f, ["supply_range"] = 1f } },
        new LeaderTrait { Id = "iron_will", Name = "Iron Will", AppliesTo = LeaderType.Admiral, Modifiers = { ["morale_resist"] = 0.25f } },
        new LeaderTrait { Id = "scout_cmd", Name = "Scout Commander", AppliesTo = LeaderType.Admiral, Modifiers = { ["detection_range"] = 0.20f, ["scout_slots"] = 1f } },
        new LeaderTrait { Id = "defensive", Name = "Defensive Expert", AppliesTo = LeaderType.Admiral, Modifiers = { ["shield_bonus"] = 0.15f, ["armor_bonus"] = 0.10f } },
    };

    public static readonly LeaderTrait[] GovernorTraits = new[]
    {
        new LeaderTrait { Id = "industrial", Name = "Industrial Planner", AppliesTo = LeaderType.Governor, Modifiers = { ["production"] = 0.20f } },
        new LeaderTrait { Id = "scientist", Name = "Chief Scientist", AppliesTo = LeaderType.Governor, Modifiers = { ["research"] = 0.20f } },
        new LeaderTrait { Id = "mining_foreman", Name = "Mining Foreman", AppliesTo = LeaderType.Governor, Modifiers = { ["mining"] = 0.20f } },
        new LeaderTrait { Id = "growth_catalyst", Name = "Growth Catalyst", AppliesTo = LeaderType.Governor, Modifiers = { ["growth"] = 0.15f } },
        new LeaderTrait { Id = "popular", Name = "Popular Leader", AppliesTo = LeaderType.Governor, Modifiers = { ["happiness"] = 15f } },
        new LeaderTrait { Id = "balanced_admin", Name = "Balanced Administrator", AppliesTo = LeaderType.Governor, Modifiers = { ["production"] = 0.08f, ["research"] = 0.08f, ["mining"] = 0.08f } },
    };
}

public class Leader
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public LeaderType Type { get; set; }
    public int OwnerEmpireId { get; set; } = -1; // -1 = in pool
    public int AssignedToId { get; set; } = -1;   // fleet or colony ID
    public List<LeaderTrait> Traits { get; set; } = new();

    public float GetModifier(string key)
    {
        float total = 0f;
        foreach (var trait in Traits)
            total += trait.Modifiers.GetValueOrDefault(key);
        return total;
    }
}

/// <summary>Manages leader pool, hiring, and assignment. Pure C#.</summary>
public class LeaderManager
{
    private readonly List<Leader> _pool = new();
    private readonly List<Leader> _hired = new();
    private int _nextLeaderId;

    public event Action<int, Leader>? LeaderHired;

    public IReadOnlyList<Leader> Pool => _pool;
    public IReadOnlyList<Leader> Hired => _hired;

    /// <summary>Generate initial leader pool.</summary>
    public void GeneratePool(int count, GameRandom rng)
    {
        for (int i = 0; i < count; i++)
        {
            bool isAdmiral = rng.Chance(0.5f);
            var leader = new Leader
            {
                Id = _nextLeaderId++,
                Name = GenerateName(rng),
                Type = isAdmiral ? LeaderType.Admiral : LeaderType.Governor,
            };

            // Assign 2-3 traits
            var traitPool = isAdmiral ? LeaderTrait.AdmiralTraits : LeaderTrait.GovernorTraits;
            int traitCount = rng.RangeInt(2, 4);
            var indices = Enumerable.Range(0, traitPool.Length).ToList();
            rng.Shuffle(indices);
            for (int t = 0; t < Math.Min(traitCount, indices.Count); t++)
                leader.Traits.Add(traitPool[indices[t]]);

            _pool.Add(leader);
        }
    }

    public Leader? HireLeader(int empireId, int leaderId)
    {
        var leader = _pool.FirstOrDefault(l => l.Id == leaderId);
        if (leader == null) return null;

        _pool.Remove(leader);
        leader.OwnerEmpireId = empireId;
        _hired.Add(leader);
        LeaderHired?.Invoke(empireId, leader);
        return leader;
    }

    public void DismissLeader(int leaderId)
    {
        var leader = _hired.FirstOrDefault(l => l.Id == leaderId);
        if (leader == null) return;
        _hired.Remove(leader);
        leader.OwnerEmpireId = -1;
        leader.AssignedToId = -1;
    }

    public bool AssignLeader(int leaderId, int targetId)
    {
        var leader = _hired.FirstOrDefault(l => l.Id == leaderId);
        if (leader == null) return false;
        leader.AssignedToId = targetId;
        return true;
    }

    public Leader? GetLeaderForTarget(int targetId) =>
        _hired.FirstOrDefault(l => l.AssignedToId == targetId);

    private static readonly string[] FirstNames = { "Kael", "Mira", "Thane", "Voss", "Lyra", "Zahn", "Seren", "Dax", "Nyx", "Orion", "Asha", "Roan", "Tessa", "Kai", "Petra" };
    private static readonly string[] LastNames = { "Strand", "Vael", "Korr", "Dynn", "Xel", "Morn", "Crest", "Falke", "Griim", "Thorn", "Sable", "Vex", "Quin", "Holt", "Raze" };

    private static string GenerateName(GameRandom rng) =>
        $"{FirstNames[rng.RangeInt(FirstNames.Length)]} {LastNames[rng.RangeInt(LastNames.Length)]}";
}
