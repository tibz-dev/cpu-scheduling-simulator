namespace SchedulerCore.Models;

public class SimulationResult
{
    public required string Algorithm { get; init; }
    public required IReadOnlyList<ProcessModel> Processes { get; init; }
    public required IReadOnlyList<ExecutionSlice> Timeline { get; init; }

    public double AverageWaitingTime =>
        Processes.Count == 0 ? 0 : Processes.Average(p => p.WaitingTime);

    public double AverageTurnaroundTime =>
        Processes.Count == 0 ? 0 : Processes.Average(p => p.TurnaroundTime);

    public double AverageResponseTime =>
        Processes.Count == 0 ? 0 : Processes.Average(p => p.ResponseTime);

    public int Makespan =>
        Processes.Count == 0 ? 0 : Processes.Max(p => p.CompletionTime ?? 0);
}
