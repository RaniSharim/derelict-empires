namespace DerlictEmpires.Core.Enums;

public enum VisibilityState
{
    Unexplored,
    Explored,
    Visible
}

/// <summary>Detection quality tier based on visibility vs detection range.</summary>
public enum DetectionLevel
{
    None,
    Minimal,    // "Something is there"
    Basic,      // Approximate fleet size, heading
    Detailed,   // Ship count by size class, role composition
    Full        // Exact designs, loadouts, supply status
}
