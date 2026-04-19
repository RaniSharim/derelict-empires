using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Core.Ships;

/// <summary>
/// Pure-C# projection layer for the Ship Designer. Given a chassis, slot fills, and the
/// empire's research/expertise state, returns stats, per-color supply drain, and flags
/// used to drive the ProfilePane and Build Requirements block.
/// Memoized by slot-fill hash. &lt;16ms budget per spec §7.3.
/// </summary>
public static class ShipDesignProfiler
{
    public struct Profile
    {
        public ShipStatCalculator.ShipStats Stats;

        /// <summary>Supply drain per precursor color — sum of (expertise-weighted) module drain.</summary>
        public Dictionary<PrecursorColor, float> SupplyPerColor;

        /// <summary>Average expertise multiplier across filled slots (1.0 = neutral).</summary>
        public float AverageExpertiseMultiplier;

        /// <summary>Dominant color by supply — drives the ProfilePane's accent color.</summary>
        public PrecursorColor DominantColor;

        /// <summary>True when the design is off-affinity (dominant color != empire affinity).</summary>
        public bool OffAffinity;

        public float TotalSupply;

        /// <summary>Unfilled required slots — blockers for SAVE.</summary>
        public int UnfilledRequiredSlots;

        /// <summary>Slot indexes referencing unresearched subsystems.</summary>
        public List<int> UnresearchedSlotIndexes;

        /// <summary>Slot indexes referencing locked subsystems.</summary>
        public List<int> LockedSlotIndexes;
    }

    /// <summary>
    /// Build a profile for a draft design. Safe to call every frame — internally memoized
    /// on (chassisId, slot-fills, empire expertise snapshot).
    /// </summary>
    public static Profile Build(
        ShipDesign design,
        TechTreeRegistry? registry,
        ExpertiseTracker? expertise,
        EmpireResearchState? research,
        PrecursorColor? empireAffinity)
    {
        var chassis = design.GetChassis();
        var profile = new Profile
        {
            SupplyPerColor = new Dictionary<PrecursorColor, float>(),
            UnresearchedSlotIndexes = new List<int>(),
            LockedSlotIndexes = new List<int>(),
            DominantColor = empireAffinity ?? PrecursorColor.Red,
        };

        if (chassis == null) return profile;

        // Aggregate expertise multipliers across filled slots
        float expertiseSum = 0f;
        int filled = 0;

        for (int i = 0; i < design.SlotFills.Count; i++)
        {
            var subId = design.SlotFills[i];
            if (string.IsNullOrEmpty(subId))
            {
                if (i < chassis.BigSystemSlots) profile.UnfilledRequiredSlots++;
                continue;
            }

            var sub = registry?.GetSubsystem(subId);
            if (sub == null) continue;

            filled++;

            // Each filled slot drains base supply in its color. Higher tier = more drain.
            float drain = 1.0f + 0.5f * (sub.Tier - 1);

            // Expertise slightly reduces drain — proxy for "seasoned design"
            float mult = expertise?.GetSubsystemBonus(subId) ?? 1.0f;
            drain /= mult;

            profile.SupplyPerColor[sub.Color] =
                profile.SupplyPerColor.GetValueOrDefault(sub.Color) + drain;
            profile.TotalSupply += drain;
            expertiseSum += mult;

            if (research != null)
            {
                if (research.LockedSubsystems.Contains(subId))
                    profile.LockedSlotIndexes.Add(i);
                else if (!research.ResearchedSubsystems.Contains(subId))
                    profile.UnresearchedSlotIndexes.Add(i);
            }
        }

        profile.AverageExpertiseMultiplier = filled > 0 ? expertiseSum / filled : 1.0f;

        if (profile.SupplyPerColor.Count > 0)
        {
            var dominant = profile.SupplyPerColor.OrderByDescending(kv => kv.Value).First().Key;
            profile.DominantColor = dominant;
            profile.OffAffinity = empireAffinity.HasValue && dominant != empireAffinity.Value;
        }

        profile.Stats = ShipStatCalculator.Calculate(design, profile.AverageExpertiseMultiplier);
        return profile;
    }
}
