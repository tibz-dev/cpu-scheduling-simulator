using SchedulerCore.Algorithms;
using SchedulerCore.Models;

namespace SchedulerCore.Services;

public class SchedulerService
{
    public SimulationResult Run(
        string algorithm,
        IEnumerable<ProcessModel> workload,
        int? roundRobinQuantum = null)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(workload);

        IScheduler scheduler = algorithm.Trim().ToUpperInvariant() switch
        {
            "FCFS" => new FcfsScheduler(),
            "SRTF" => new SrtfScheduler(),
            "ROUND ROBIN" or "RR" =>
                new RoundRobinScheduler(
                    roundRobinQuantum ??
                    throw new ArgumentException(
                        "Round Robin requires a quantum.",
                        nameof(roundRobinQuantum))),
            _ => throw new ArgumentException(
                $"Unknown scheduling algorithm '{algorithm}'.",
                nameof(algorithm))
        };

        return scheduler.Run(workload);
    }
}
