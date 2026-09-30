using SchedulerCore.Metrics;
using SchedulerCore.Models;
using SchedulerCore.Simulation;

namespace SchedulerCore.Algorithms;

public class SrtfScheduler : IScheduler
{
    public string Name => "SRTF";

    public SimulationResult Run(IEnumerable<ProcessModel> processes)
    {
        List<ProcessModel> working = processes
            .Select(p => p.Clone())
            .OrderBy(p => p.ArrivalTime)
            .ThenBy(p => p.ProcessId)
            .ToList();

        var timeline = new List<ExecutionSlice>();

        if (working.Count == 0)
        {
            return new SimulationResult
            {
                Algorithm = Name,
                Processes = working,
                Timeline = timeline
            };
        }

        int currentTime = working.Min(p => p.ArrivalTime);
        int completed = 0;

        while (completed < working.Count)
        {
            ProcessModel? current = working
                .Where(p => p.ArrivalTime <= currentTime && p.RemainingTime > 0)
                .OrderBy(p => p.RemainingTime)
                .ThenBy(p => p.ArrivalTime)
                .ThenBy(p => p.ProcessId)
                .FirstOrDefault();

            if (current is null)
            {
                currentTime = working
                    .Where(p => p.RemainingTime > 0)
                    .Min(p => p.ArrivalTime);

                continue;
            }

            current.FirstStartTime ??= currentTime;

            TimelineHelper.Add(
                timeline,
                current.ProcessId,
                currentTime,
                currentTime + 1);

            current.RemainingTime--;
            currentTime++;

            if (current.RemainingTime == 0)
            {
                current.CompletionTime = currentTime;
                completed++;
            }
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
