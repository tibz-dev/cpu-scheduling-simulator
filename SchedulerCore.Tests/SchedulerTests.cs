using SchedulerCore.Algorithms;
using SchedulerCore.Models;
using SchedulerCore.Services;

namespace SchedulerCore.Tests;

[TestClass]
public class SchedulerTests
{
    [TestMethod]
    public void WorkloadGenerator_IsDeterministic()
    {
        var generator = new WorkloadGenerator();

        var first = generator.Generate(10, 10001);
        var second = generator.Generate(10, 10001);

        CollectionAssert.AreEqual(
            first.Select(p => (p.ArrivalTime, p.BurstTime, p.Priority)).ToList(),
            second.Select(p => (p.ArrivalTime, p.BurstTime, p.Priority)).ToList());
    }

    [TestMethod]
    public void Fcfs_ComputesExpectedMetrics()
    {
        var workload = new[]
        {
            P(1, 0, 5),
            P(2, 1, 3),
            P(3, 2, 1)
        };

        var result = new FcfsScheduler().Run(workload);

        AssertProcess(result, 1, start: 0, completion: 5, waiting: 0, turnaround: 5, response: 0);
        AssertProcess(result, 2, start: 5, completion: 8, waiting: 4, turnaround: 7, response: 4);
        AssertProcess(result, 3, start: 8, completion: 9, waiting: 6, turnaround: 7, response: 6);
    }

    [TestMethod]
    public void Srtf_PreemptsWhenShorterJobArrives()
    {
        var workload = new[]
        {
            P(1, 0, 8),
            P(2, 1, 4),
            P(3, 2, 2)
        };

        var result = new SrtfScheduler().Run(workload);

        AssertProcess(result, 1, start: 0, completion: 14, waiting: 6, turnaround: 14, response: 0);
        AssertProcess(result, 2, start: 1, completion: 7, waiting: 2, turnaround: 6, response: 0);
        AssertProcess(result, 3, start: 2, completion: 4, waiting: 0, turnaround: 2, response: 0);
    }

    [TestMethod]
    public void RoundRobin_UsesQuantumAndCompletesAllProcesses()
    {
        var workload = new[]
        {
            P(1, 0, 5),
            P(2, 0, 3)
        };

        var result = new RoundRobinScheduler(2).Run(workload);

        AssertProcess(result, 1, start: 0, completion: 8, waiting: 3, turnaround: 8, response: 0);
        AssertProcess(result, 2, start: 2, completion: 7, waiting: 4, turnaround: 7, response: 2);
    }

    [TestMethod]
    public void SchedulerService_DoesNotMutateOriginalWorkload()
    {
        var workload = new List<ProcessModel>
        {
            P(1, 0, 4),
            P(2, 1, 2)
        };

        var service = new SchedulerService();
        _ = service.Run("FCFS", workload);

        Assert.AreEqual(4, workload[0].RemainingTime);
        Assert.AreEqual(2, workload[1].RemainingTime);
        Assert.IsNull(workload[0].CompletionTime);
        Assert.IsNull(workload[1].CompletionTime);
    }

    private static ProcessModel P(int id, int arrival, int burst) =>
        new()
        {
            ProcessId = id,
            ArrivalTime = arrival,
            BurstTime = burst,
            Priority = 1,
            RemainingTime = burst
        };

    private static void AssertProcess(
        SimulationResult result,
        int id,
        int start,
        int completion,
        int waiting,
        int turnaround,
        int response)
    {
        ProcessModel process = result.Processes.Single(p => p.ProcessId == id);

        Assert.AreEqual(start, process.FirstStartTime);
        Assert.AreEqual(completion, process.CompletionTime);
        Assert.AreEqual(waiting, process.WaitingTime);
        Assert.AreEqual(turnaround, process.TurnaroundTime);
        Assert.AreEqual(response, process.ResponseTime);
    }
}
