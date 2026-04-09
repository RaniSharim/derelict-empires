namespace DerlictEmpires.Core.Enums;

/// <summary>Empire origin templates determining starting role and advantages.</summary>
public enum Origin
{
    Warriors,    // Military — combat bonuses, extra Fighter
    Servitors,   // Research/Maintenance — research speed, extra Salvager
    Haulers,     // Logistics/Trade — see hidden lanes, extra Scout
    Chroniclers, // Research/Color — deeper color research, extra Scout
    FreeRace     // Independent — no affinity, balanced, extra Builder
}
