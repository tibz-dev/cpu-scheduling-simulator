using SchedulerCore.Models;

namespace SchedulerCore.Services;

public class WorkloadGenerator
{
    public const int MinBurstTime = 1;
    public const int MaxBurstTime = 20;
    public const int MinArrivalGap = 0;
    public const int MaxArrivalGap = 5;
    public const int MinPriority = 1;
    public const int MaxPriority = 5;

    public IReadOnlyList<ProcessModel> Generate(int processCount, int seed)
    {
        if (processCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(processCount),
                "Process count must be greater than zero.");

        var random = new Random(seed);
        var processes = new List<ProcessModel>(processCount);
        int arrivalTime = 0;

        for (int i = 0; i < processCount; i++)
        {
            if (i > 0)
                arrivalTime += random.Next(MinArrivalGap, MaxArrivalGap + 1);

            int burstTime = random.Next(MinBurstTime, MaxBurstTime + 1);
            int priority = random.Next(MinPriority, MaxPriority + 1);

            processes.Add(new ProcessModel
            {
                ProcessId = i + 1,
                ArrivalTime = arrivalTime,
                BurstTime = burstTime,
                Priority = priority,
                RemainingTime = burstTime
            });
        }

        return processes;
    }
}
