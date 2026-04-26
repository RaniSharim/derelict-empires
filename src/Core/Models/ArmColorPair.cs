using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// Two-color identity for a spiral arm. Primary = arm's "home" color (what
/// systems in the arm dominantly inherit). Secondary = neighboring arm's
/// home color, blending arms at their seams. Used by the salvage color
/// roller (30/30 base weight, blended toward uniform near the core).
/// </summary>
public readonly record struct ArmColorPair(PrecursorColor Primary, PrecursorColor Secondary);
