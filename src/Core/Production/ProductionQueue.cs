using System;
using System.Collections.Generic;

namespace DerlictEmpires.Core.Production;

/// <summary>
/// Generic ordered production queue. Items progress based on production points
/// invested per tick. Overflow carries to the next item.
/// Reusable across colonies, stations, shipyards.
/// </summary>
public class ProductionQueue
{
    public class QueueEntry
    {
        public IProducible Item { get; set; } = null!;
        public int Invested { get; set; }
        public int TotalCost => Item.ProductionCost;
        public float Progress => TotalCost > 0 ? (float)Invested / TotalCost : 1f;
        public bool IsComplete => Invested >= TotalCost;
    }

    private readonly List<QueueEntry> _queue = new();

    public event Action<IProducible>? ItemCompleted;

    public IReadOnlyList<QueueEntry> Entries => _queue;
    public QueueEntry? Current => _queue.Count > 0 ? _queue[0] : null;
    public int Count => _queue.Count;
    public bool IsEmpty => _queue.Count == 0;

    /// <summary>Add an item to the end of the queue.</summary>
    public void Enqueue(IProducible item)
    {
        _queue.Add(new QueueEntry { Item = item });
    }

    /// <summary>Remove an item at a specific index.</summary>
    public void RemoveAt(int index)
    {
        if (index >= 0 && index < _queue.Count)
            _queue.RemoveAt(index);
    }

    /// <summary>Clear the entire queue.</summary>
    public void Clear() => _queue.Clear();

    /// <summary>
    /// Invest production points. Returns a list of completed items.
    /// Overflow from one completion carries to the next queued item.
    /// </summary>
    public List<IProducible> ProcessTick(int productionPoints)
    {
        var completed = new List<IProducible>();
        int remaining = productionPoints;

        while (remaining > 0 && _queue.Count > 0)
        {
            var entry = _queue[0];
            int needed = entry.TotalCost - entry.Invested;
            int applied = Math.Min(remaining, needed);

            entry.Invested += applied;
            remaining -= applied;

            if (entry.IsComplete)
            {
                completed.Add(entry.Item);
                ItemCompleted?.Invoke(entry.Item);
                _queue.RemoveAt(0);
            }
        }

        return completed;
    }
}
