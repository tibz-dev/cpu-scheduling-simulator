using SchedulerCore.Models;

namespace SchedulerCore.Metrics;

public static class MetricsCalculator
{
    public static void Populate(IEnumerable<ProcessModel> processes)
    {
        foreach (ProcessModel process in processes)
        {
            if (process.FirstStartTime is null)
                throw new InvalidOperationException(
                    $"Process {process.ProcessId} has no first start time.");

            if (process.CompletionTime is null)
                throw new InvalidOperationException(
                    $"Process {process.ProcessId} has no completion time.");

            process.TurnaroundTime =
                process.CompletionTime.Value - process.ArrivalTime;

            process.WaitingTime =
                process.TurnaroundTime - process.BurstTime;

            process.ResponseTime =
                process.FirstStartTime.Value - process.ArrivalTime;
        }
    }
}
