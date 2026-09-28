namespace SchedulerCore.Models;

public class ExecutionSlice
{
    public int ProcessId { get; init; }
    public int StartTime { get; init; }
    public int EndTime { get; set; }
    public int Duration => EndTime - StartTime;
}
