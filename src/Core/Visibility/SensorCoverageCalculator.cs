using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Stations;

namespace DerlictEmpires.Core.Visibility;

/// <summary>Per-band sensor coverage for a given empire in a given system. See design/in_system_design.md §3.4, §6.4.</summary>
public sealed class BandCoverage
{
    public Band Band { get; init; }
    public int  Coverage { get; init; }
    public List<string> Sources { get; init; } = new();

    /// <summary>Names of assets whose removal would drop this band to zero — surfaced as "via {source}" in UI.</summary>
    public string? SoleSource =>
        Coverage > 0 && Sources.Count == 1 ? Sources[0] : null;
}

/// <summary>
/// Rolls up sensor-equipped player assets into per-band coverage numbers. v1 keeps the math
/// coarse: stations with sensors contribute their SensorPower to all three bands, colonies
/// contribute a baseline to Inner only (where they sit), fleets contribute nothing until fleet
/// sensors ship. Cleaner math comes with the Detection design pass.
/// </summary>
public static class SensorCoverageCalculator
{
    public static Dictionary<Band, BandCoverage> Compute(
        int empireId,
        int systemId,
        IReadOnlyList<Station>? stations,
        IReadOnlyList<DerlictEmpires.Core.Settlements.Colony>? colonies)
    {
        var result = new Dictionary<Band, BandCoverage>
        {
            [Band.Inner] = new BandCoverage { Band = Band.Inner },
            [Band.Mid]   = new BandCoverage { Band = Band.Mid   },
            [Band.Outer] = new BandCoverage { Band = Band.Outer },
        };

        int innerCov = 0, midCov = 0, outerCov = 0;
        var innerSrc = new List<string>();
        var midSrc   = new List<string>();
        var outerSrc = new List<string>();

        if (stations != null)
        {
            foreach (var s in stations)
            {
                if (s.SystemId != systemId || s.OwnerEmpireId != empireId) continue;
                int sensorSum = 0;
                foreach (var m in s.Modules)
                    if (m is SensorModule sm) sensorSum += (int)sm.SensorPower;
                if (sensorSum <= 0) continue;
                // Stations cover all three bands at full power.
                innerCov += sensorSum; innerSrc.Add(s.Name);
                midCov   += sensorSum; midSrc  .Add(s.Name);
                outerCov += sensorSum; outerSrc.Add(s.Name);
            }
        }

        if (colonies != null)
        {
            foreach (var c in colonies)
            {
                if (c.SystemId != systemId || c.OwnerEmpireId != empireId) continue;
                // Colonies saturate Inner-band coverage around themselves.
                innerCov += 20;
                innerSrc.Add(c.Name);
            }
        }

        return new Dictionary<Band, BandCoverage>
        {
            [Band.Inner] = new BandCoverage { Band = Band.Inner, Coverage = innerCov, Sources = innerSrc },
            [Band.Mid]   = new BandCoverage { Band = Band.Mid,   Coverage = midCov,   Sources = midSrc   },
            [Band.Outer] = new BandCoverage { Band = Band.Outer, Coverage = outerCov, Sources = outerSrc },
        };
    }
}
