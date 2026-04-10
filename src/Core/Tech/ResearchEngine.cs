using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Tech;

/// <summary>
/// Processes research per slow tick. Applies efficiency multipliers.
/// Checks for tier/subsystem completion. Pure C#.
/// </summary>
public class ResearchEngine
{
    public event Action<int, string>? SubsystemResearched; // empireId, subsystemId
    public event Action<int, PrecursorColor, TechCategory, int>? TierUnlocked; // empireId, color, cat, tier
    public event Action<int, string>? SynergyAvailable; // empireId, synergyId
    public event Action<int, string>? SynergyResearched; // empireId, synergyId

    private readonly TechTreeRegistry _registry;

    public ResearchEngine(TechTreeRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Process one slow tick of research for an empire.
    /// </summary>
    public void ProcessTick(
        EmpireResearchState state,
        PrecursorColor? empireAffinity,
        float researchOutput,
        float tickDelta,
        GameRandom rng)
    {
        if (state.CurrentProject == null)
        {
            // Try to pull from queue
            if (state.Queue.Count > 0)
            {
                state.CurrentProject = state.Queue[0];
                state.Queue.RemoveAt(0);
                state.CurrentProgress = 0f;
            }
            else return;
        }

        // Determine project type and cost
        var subsystem = _registry.GetSubsystem(state.CurrentProject);
        var synergy = _registry.Synergies.Find(s => s.Id == state.CurrentProject);

        float cost;
        PrecursorColor techColor;

        if (subsystem != null)
        {
            cost = subsystem.ResearchCost;
            techColor = subsystem.Color;
        }
        else if (synergy != null)
        {
            cost = synergy.ResearchCost;
            techColor = synergy.ColorA; // Use primary color for efficiency
        }
        else
        {
            // Unknown project — skip
            state.CurrentProject = null;
            state.CurrentProgress = 0f;
            return;
        }

        // Apply efficiency
        float efficiency = EfficiencyCalculator.GetEfficiency(empireAffinity, techColor);
        float points = researchOutput * efficiency * tickDelta;
        state.CurrentProgress += points;

        if (state.CurrentProgress >= cost)
        {
            // Project complete
            if (subsystem != null)
            {
                state.CompleteSubsystem(subsystem.Id);
                SubsystemResearched?.Invoke(state.EmpireId, subsystem.Id);

                // Check if this completes a tier, unlocking the next
                CheckTierUnlock(state, subsystem, rng);
            }
            else if (synergy != null)
            {
                state.ResearchedSynergies.Add(synergy.Id);
                state.AvailableSynergies.Remove(synergy.Id);
                SynergyResearched?.Invoke(state.EmpireId, synergy.Id);
            }

            state.CurrentProject = null;
            state.CurrentProgress = 0f;

            // Check for newly available synergies
            CheckSynergyUnlocks(state);
        }
    }

    private void CheckTierUnlock(EmpireResearchState state, SubsystemData subsystem, GameRandom rng)
    {
        // A tier is "complete enough" to unlock the next when all available subsystems
        // in the current tier are researched
        var currentTier = state.GetUnlockedTier(subsystem.Color, subsystem.Category);
        if (subsystem.Tier != currentTier) return; // Only check current tier

        var node = _registry.GetNode(subsystem.Color, subsystem.Category, currentTier);
        if (node == null) return;

        // Check if at least 2 of the 3 subsystems are researched
        int researched = 0;
        foreach (var subId in node.SubsystemIds)
            if (state.HasSubsystem(subId)) researched++;

        if (researched >= 2 && currentTier < 6)
        {
            int nextTier = currentTier + 1;
            var nextNode = _registry.GetNode(subsystem.Color, subsystem.Category, nextTier);
            if (nextNode != null)
            {
                var childRng = rng.DeriveChild(state.EmpireId * 1000 + nextTier * 100 +
                    (int)subsystem.Color * 10 + (int)subsystem.Category);
                state.UnlockTier(subsystem.Color, subsystem.Category, nextTier, nextNode, childRng);
                TierUnlocked?.Invoke(state.EmpireId, subsystem.Color, subsystem.Category, nextTier);
            }
        }
    }

    /// <summary>Check if any synergy techs are now available based on tier levels.</summary>
    public void CheckSynergyUnlocks(EmpireResearchState state)
    {
        foreach (var synergy in _registry.Synergies)
        {
            if (state.AvailableSynergies.Contains(synergy.Id)) continue;
            if (state.ResearchedSynergies.Contains(synergy.Id)) continue;

            // Check if both required tiers are met (across any category)
            bool metA = false, metB = false;
            foreach (var category in Enum.GetValues<TechCategory>())
            {
                if (state.GetUnlockedTier(synergy.ColorA, category) >= synergy.RequiredTierA) metA = true;
                if (state.GetUnlockedTier(synergy.ColorB, category) >= synergy.RequiredTierB) metB = true;
            }

            if (metA && metB)
            {
                state.AvailableSynergies.Add(synergy.Id);
                SynergyAvailable?.Invoke(state.EmpireId, synergy.Id);
            }
        }
    }
}
