using SchedulerCore.Metrics;
using SchedulerCore.Models;
using SchedulerCore.Simulation;

namespace SchedulerCore.Algorithms;

public class RoundRobinScheduler : IScheduler
{
    public string Name => "Round Robin";
    public int Quantum { get; }

    public RoundRobinScheduler(int quantum)
    {
        if (quantum <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantum),
                "Round Robin quantum must be greater than zero.");

        Quantum = quantum;
    }

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

        var ready = new Queue<ProcessModel>();
        int nextIndex = 0;
        int currentTime = working[0].ArrivalTime;

        EnqueueArrivals();

        while (ready.Count > 0 || nextIndex < working.Count)
        {
            if (ready.Count == 0)
            {
                currentTime = Math.Max(currentTime, working[nextIndex].ArrivalTime);
                EnqueueArrivals();
            }

            ProcessModel current = ready.Dequeue();
            current.FirstStartTime ??= currentTime;

            int runFor = Math.Min(Quantum, current.RemainingTime);
            int start = currentTime;

            for (int i = 0; i < runFor; i++)
            {
                current.RemainingTime--;
                currentTime++;

                EnqueueArrivals();

                if (current.RemainingTime == 0)
                    break;
            }

            TimelineHelper.Add(
                timeline,
                current.ProcessId,
                start,
                currentTime);

            if (current.RemainingTime == 0)
            {
                current.CompletionTime = currentTime;
            }
            else
            {
                ready.Enqueue(current);
            }
        }

        MetricsCalculator.Populate(working);

        return new SimulationResult
        {
            Algorithm = Name,
            Processes = working.OrderBy(p => p.ProcessId).ToList(),
            Timeline = timeline
        };

        void EnqueueArrivals()
        {
            while (nextIndex < working.Count &&
                   working[nextIndex].ArrivalTime <= currentTime)
            {
                ready.Enqueue(working[nextIndex]);
                nextIndex++;
            }
        }
    }
}
