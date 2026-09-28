using SchedulerCore.Metrics;
using SchedulerCore.Models;
using SchedulerCore.Simulation;

namespace SchedulerCore.Algorithms;

public class FcfsScheduler : IScheduler
{
    public string Name => "FCFS";

    public SimulationResult Run(IEnumerable<ProcessModel> processes)
    {
        List<ProcessModel> working = processes
            .Select(p => p.Clone())
            .OrderBy(p => p.ArrivalTime)
            .ThenBy(p => p.ProcessId)
            .ToList();

        var timeline = new List<ExecutionSlice>();
        int currentTime = 0;

        foreach (ProcessModel process in working)
        {
            if (currentTime < process.ArrivalTime)
                currentTime = process.ArrivalTime;

            process.FirstStartTime = currentTime;

            int start = currentTime;
            currentTime += process.BurstTime;

            process.RemainingTime = 0;
            process.CompletionTime = currentTime;

            TimelineHelper.Add(
                timeline,
                process.ProcessId,
                start,
                currentTime);
        }

        MetricsCalculator.Populate(working);

        return new SimulationResult
        {
            Algorithm = Name,
            Processes = working.OrderBy(p => p.ProcessId).ToList(),
            Timeline = timeline
        };
    }
}
