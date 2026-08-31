using SchedulerCore.Models;

namespace SchedulerCore.Tests;

[TestClass]
public class ProcessModelTests
{
    [TestMethod]
    public void NewProcess_HasNullTimestamps_UntilSet()
    {
        // A freshly created process hasn't run yet — FirstStartTime and
        // CompletionTime must be null, not 0, or metrics calculations
        // downstream could mistake "never ran" for "ran at time zero".
        var process = new ProcessModel
        {
            ProcessId = 1,
            ArrivalTime = 0,
            BurstTime = 5,
            Priority = 3,
            RemainingTime = 5
        };

        Assert.IsNull(process.FirstStartTime);
        Assert.IsNull(process.CompletionTime);
    }

    [TestMethod]
    public void Clone_ProducesIndependentCopy_WithResetSimulationState()
    {
        var original = new ProcessModel
        {
            ProcessId = 7,
            ArrivalTime = 4,
            BurstTime = 10,
            Priority = 2,
            RemainingTime = 3,       // pretend it already ran partway
            FirstStartTime = 4,
            CompletionTime = 20,
            WaitingTime = 6,
            TurnaroundTime = 16,
            ResponseTime = 0
        };

        var clone = original.Clone();

        // Static process attributes must carry over unchanged.
        Assert.AreEqual(original.ProcessId, clone.ProcessId);
        Assert.AreEqual(original.ArrivalTime, clone.ArrivalTime);
        Assert.AreEqual(original.BurstTime, clone.BurstTime);
        Assert.AreEqual(original.Priority, clone.Priority);

        // Simulation state must be reset, not copied.
        Assert.AreEqual(clone.BurstTime, clone.RemainingTime);
        Assert.IsNull(clone.FirstStartTime);
        Assert.IsNull(clone.CompletionTime);
        Assert.AreEqual(0, clone.WaitingTime);
        Assert.AreEqual(0, clone.TurnaroundTime);
        Assert.AreEqual(0, clone.ResponseTime);
    }

    [TestMethod]
    public void Clone_DoesNotAffectOriginal_WhenCloneIsMutated()
    {
        // Guards against Clone() accidentally returning a reference
        // instead of a true independent copy.
        var original = new ProcessModel
        {
            ProcessId = 1,
            ArrivalTime = 0,
            BurstTime = 8,
            RemainingTime = 8
        };

        var clone = original.Clone();
        clone.RemainingTime = 0;
        clone.CompletionTime = 8;

        Assert.AreEqual(8, original.RemainingTime);
        Assert.IsNull(original.CompletionTime);
    }
}