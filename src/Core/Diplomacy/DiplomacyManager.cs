using System;
using System.Collections.Generic;
using System.Linq;

namespace DerlictEmpires.Core.Diplomacy;

public enum AgreementType { NonAggression, Alliance, Trade, Rights, TechRental, Team }
public enum AgreementStatus { Proposed, Active, Broken, Expired }

public class DiplomaticAgreement
{
    public int Id { get; set; }
    public AgreementType Type { get; set; }
    public int PartyA { get; set; }
    public int PartyB { get; set; }
    public AgreementStatus Status { get; set; } = AgreementStatus.Proposed;
    public int StartTick { get; set; }
    public int Duration { get; set; } = -1; // -1 = perpetual
}

/// <summary>
/// Manages diplomatic contact, agreements, and reputation between empires. Pure C#.
/// </summary>
public class DiplomacyManager
{
    private readonly HashSet<(int, int)> _contacts = new();
    private readonly List<DiplomaticAgreement> _agreements = new();
    private readonly ReputationSystem _reputation = new();
    private int _nextAgreementId;

    public event Action<int, int>? FirstContactEstablished;
    public event Action<DiplomaticAgreement>? AgreementAccepted;
    public event Action<DiplomaticAgreement, int>? AgreementBroken; // agreement, breakerEmpireId

    public ReputationSystem Reputation => _reputation;
    public IReadOnlyList<DiplomaticAgreement> Agreements => _agreements;

    public bool HasContact(int empireA, int empireB) =>
        _contacts.Contains((Math.Min(empireA, empireB), Math.Max(empireA, empireB)));

    public void EstablishContact(int empireA, int empireB)
    {
        var key = (Math.Min(empireA, empireB), Math.Max(empireA, empireB));
        if (_contacts.Add(key))
            FirstContactEstablished?.Invoke(empireA, empireB);
    }

    public DiplomaticAgreement ProposeAgreement(AgreementType type, int from, int to, int currentTick, int duration = -1)
    {
        var agreement = new DiplomaticAgreement
        {
            Id = _nextAgreementId++,
            Type = type,
            PartyA = from,
            PartyB = to,
            StartTick = currentTick,
            Duration = duration
        };
        _agreements.Add(agreement);
        return agreement;
    }

    public bool AcceptAgreement(int agreementId)
    {
        var agreement = _agreements.FirstOrDefault(a => a.Id == agreementId && a.Status == AgreementStatus.Proposed);
        if (agreement == null) return false;
        agreement.Status = AgreementStatus.Active;
        _reputation.ModifyRelation(agreement.PartyA, agreement.PartyB, 10f);
        AgreementAccepted?.Invoke(agreement);
        return true;
    }

    public bool BreakAgreement(int agreementId, int breakerEmpireId)
    {
        var agreement = _agreements.FirstOrDefault(a => a.Id == agreementId && a.Status == AgreementStatus.Active);
        if (agreement == null) return false;
        agreement.Status = AgreementStatus.Broken;

        // Reputation hit
        float penalty = agreement.Type switch
        {
            AgreementType.NonAggression => -30f,
            AgreementType.Alliance => -50f,
            AgreementType.Trade => -15f,
            _ => -20f
        };
        _reputation.ApplyReputationChange(breakerEmpireId, penalty);

        int otherId = agreement.PartyA == breakerEmpireId ? agreement.PartyB : agreement.PartyA;
        _reputation.ModifyRelation(breakerEmpireId, otherId, penalty);

        AgreementBroken?.Invoke(agreement, breakerEmpireId);
        return true;
    }

    public List<DiplomaticAgreement> GetActiveAgreements(int empireId) =>
        _agreements.Where(a => a.Status == AgreementStatus.Active &&
            (a.PartyA == empireId || a.PartyB == empireId)).ToList();

    public bool HasAgreement(int empireA, int empireB, AgreementType type) =>
        _agreements.Any(a => a.Status == AgreementStatus.Active && a.Type == type &&
            ((a.PartyA == empireA && a.PartyB == empireB) ||
             (a.PartyA == empireB && a.PartyB == empireA)));
}

/// <summary>
/// Tracks per-empire reliability (0-100) and per-pair relation (-100 to +100).
/// </summary>
public class ReputationSystem
{
    private readonly Dictionary<int, float> _reliability = new(); // 0-100, starts 50
    private readonly Dictionary<(int, int), float> _relations = new(); // -100 to +100

    public float GetReliability(int empireId) =>
        _reliability.GetValueOrDefault(empireId, 50f);

    public float GetRelation(int empireA, int empireB)
    {
        var key = (Math.Min(empireA, empireB), Math.Max(empireA, empireB));
        return _relations.GetValueOrDefault(key);
    }

    public void ApplyReputationChange(int empireId, float delta)
    {
        float current = GetReliability(empireId);
        _reliability[empireId] = Math.Clamp(current + delta, 0f, 100f);
    }

    public void ModifyRelation(int empireA, int empireB, float delta)
    {
        var key = (Math.Min(empireA, empireB), Math.Max(empireA, empireB));
        float current = _relations.GetValueOrDefault(key);
        _relations[key] = Math.Clamp(current + delta, -100f, 100f);
    }

    public void DecayTowardsNeutral(float rate = 0.5f)
    {
        foreach (var key in _relations.Keys.ToList())
        {
            float current = _relations[key];
            if (MathF.Abs(current) < rate) _relations[key] = 0f;
            else _relations[key] = current > 0 ? current - rate : current + rate;
        }
    }
}
