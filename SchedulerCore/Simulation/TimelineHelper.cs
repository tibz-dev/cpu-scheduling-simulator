using SchedulerCore.Models;

namespace SchedulerCore.Simulation;

internal static class TimelineHelper
{
    public static void Add(
        List<ExecutionSlice> timeline,
        int processId,
        int startTime,
        int endTime)
    {
        if (endTime <= startTime)
            return;

        ExecutionSlice? last = timeline.LastOrDefault();

        if (last is not null &&
            last.ProcessId == processId &&
            last.EndTime == startTime)
        {
            last.EndTime = endTime;
            return;
        }

        timeline.Add(new ExecutionSlice
        {
            ProcessId = processId,
            StartTime = startTime,
            EndTime = endTime
        });
    }
}
