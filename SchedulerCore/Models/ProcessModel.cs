namespace SchedulerCore.Models;

/// <summary>
/// Represents a single process to be scheduled. Arrival/Burst/Priority are
/// fixed at generation time (see WorkloadGenerator, Phase 6). RemainingTime,
/// FirstStartTime and CompletionTime are mutated by the scheduling algorithms
/// during simulation. WaitingTime/TurnaroundTime/ResponseTime are populated
/// afterwards by MetricsCalculator (Phase 10) — algorithms never set these directly.
/// </summary>
public class ProcessModel
{
    public int ProcessId { get; set; }
    public int ArrivalTime { get; set; }
    public int BurstTime { get; set; }
    public int Priority { get; set; }

    // Mutated during simulation — starts equal to BurstTime, decremented
    // by SRTF/RR as the process executes. FCFS never preempts, so it runs
    // this straight to zero in one go.
    public int RemainingTime { get; set; }

    // Null until the process is first dispatched to the CPU.
    public int? FirstStartTime { get; set; }

    // Null until the process finishes all of its burst time.
    public int? CompletionTime { get; set; }

    // Populated by MetricsCalculator after the simulation run completes.
    public int WaitingTime { get; set; }
    public int TurnaroundTime { get; set; }
    public int ResponseTime { get; set; }

    /// <summary>
    /// Returns a fresh copy of this process with simulation state reset
    /// (RemainingTime = BurstTime, timestamps cleared). Used because the
    /// same Workload must be run against three different algorithms —
    /// without cloning, running FCFS would mutate RemainingTime and
    /// leave SRTF starting from an already-decremented state.
    /// </summary>
    public ProcessModel Clone()
    {
        return new ProcessModel
        {
            ProcessId = ProcessId,
            ArrivalTime = ArrivalTime,
            BurstTime = BurstTime,
            Priority = Priority,
            RemainingTime = BurstTime,
            FirstStartTime = null,
            CompletionTime = null,
            WaitingTime = 0,
            TurnaroundTime = 0,
            ResponseTime = 0
        };
    }
}