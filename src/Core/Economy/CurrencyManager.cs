using System;
using System.Collections.Generic;

namespace DerlictEmpires.Core.Economy;

/// <summary>Per-empire credit tracking with income/expense ledger. Pure C#.</summary>
public class CurrencyManager
{
    private readonly Dictionary<int, long> _balances = new();
    public event Action<int, long, long>? CreditsChanged; // empireId, newBalance, delta

    public long GetBalance(int empireId) => _balances.GetValueOrDefault(empireId);

    public void SetBalance(int empireId, long amount) => _balances[empireId] = amount;

    public void AddCredits(int empireId, long amount)
    {
        long old = GetBalance(empireId);
        _balances[empireId] = old + amount;
        CreditsChanged?.Invoke(empireId, old + amount, amount);
    }

    public bool SpendCredits(int empireId, long amount)
    {
        long balance = GetBalance(empireId);
        if (balance < amount) return false;
        _balances[empireId] = balance - amount;
        CreditsChanged?.Invoke(empireId, balance - amount, -amount);
        return true;
    }
}
