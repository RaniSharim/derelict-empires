using Xunit;
using System.Collections.Generic;
using DerlictEmpires.Core.Production;

namespace DerlictEmpires.Tests.Settlements;

public class ProductionQueueTests
{
    private class TestItem : IProducible
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int ProductionCost { get; set; }
    }

    [Fact]
    public void Enqueue_AddsItems()
    {
        var queue = new ProductionQueue();
        queue.Enqueue(new TestItem { Id = "a", ProductionCost = 10 });
        queue.Enqueue(new TestItem { Id = "b", ProductionCost = 20 });

        Assert.Equal(2, queue.Count);
        Assert.Equal("a", queue.Current!.Item.Id);
    }

    [Fact]
    public void ProcessTick_InvestsPoints()
    {
        var queue = new ProductionQueue();
        queue.Enqueue(new TestItem { Id = "a", ProductionCost = 100 });

        queue.ProcessTick(30);

        Assert.Equal(30, queue.Current!.Invested);
        Assert.False(queue.Current.IsComplete);
    }

    [Fact]
    public void ProcessTick_CompletesItem()
    {
        var queue = new ProductionQueue();
        queue.Enqueue(new TestItem { Id = "a", ProductionCost = 50 });

        var completed = queue.ProcessTick(50);

        Assert.Single(completed);
        Assert.Equal("a", completed[0].Id);
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void Overflow_CarriesToNextItem()
    {
        var queue = new ProductionQueue();
        queue.Enqueue(new TestItem { Id = "a", ProductionCost = 30 });
        queue.Enqueue(new TestItem { Id = "b", ProductionCost = 50 });

        var completed = queue.ProcessTick(40); // 30 to complete a, 10 overflow to b

        Assert.Single(completed);
        Assert.Equal("a", completed[0].Id);
        Assert.Equal(1, queue.Count);
        Assert.Equal(10, queue.Current!.Invested);
    }

    [Fact]
    public void MultipleCompletions_InOneTick()
    {
        var queue = new ProductionQueue();
        queue.Enqueue(new TestItem { Id = "a", ProductionCost = 10 });
        queue.Enqueue(new TestItem { Id = "b", ProductionCost = 10 });
        queue.Enqueue(new TestItem { Id = "c", ProductionCost = 10 });

        var completed = queue.ProcessTick(25);

        Assert.Equal(2, completed.Count);
        Assert.Equal("a", completed[0].Id);
        Assert.Equal("b", completed[1].Id);
        Assert.Equal(1, queue.Count);
        Assert.Equal(5, queue.Current!.Invested);
    }

    [Fact]
    public void ItemCompleted_EventFires()
    {
        var queue = new ProductionQueue();
        var completedIds = new List<string>();
        queue.ItemCompleted += item => completedIds.Add(item.Id);

        queue.Enqueue(new TestItem { Id = "x", ProductionCost = 10 });
        queue.ProcessTick(10);

        Assert.Single(completedIds);
        Assert.Equal("x", completedIds[0]);
    }

    [Fact]
    public void EmptyQueue_ProcessTick_DoesNothing()
    {
        var queue = new ProductionQueue();
        var completed = queue.ProcessTick(100);
        Assert.Empty(completed);
    }

    [Fact]
    public void RemoveAt_RemovesItem()
    {
        var queue = new ProductionQueue();
        queue.Enqueue(new TestItem { Id = "a", ProductionCost = 10 });
        queue.Enqueue(new TestItem { Id = "b", ProductionCost = 20 });

        queue.RemoveAt(0);

        Assert.Equal(1, queue.Count);
        Assert.Equal("b", queue.Current!.Item.Id);
    }

    [Fact]
    public void Progress_CalculatesCorrectly()
    {
        var queue = new ProductionQueue();
        queue.Enqueue(new TestItem { Id = "a", ProductionCost = 100 });
        queue.ProcessTick(25);

        Assert.Equal(0.25f, queue.Current!.Progress);
    }
}
