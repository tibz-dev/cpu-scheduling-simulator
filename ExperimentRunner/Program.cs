using SchedulerCore.Models;
using SchedulerCore.Services;

int[] processCounts = { 10, 20, 30, 40, 50 };
int[] trials = { 1, 2, 3, 4, 5 };
string[] algorithms = { "FCFS", "SRTF", "Round Robin" };

// The research methodology requires one fixed RR quantum per workload.
// It is deterministically derived from the workload seed so it is reproducible
// and always falls within the required range 2-8.
const int minQuantum = 2;
const int maxQuantum = 8;

var workloadGenerator = new WorkloadGenerator();
var schedulerService = new SchedulerService();

Console.WriteLine("CPU Scheduling Simulator - Experiment Runner");
Console.WriteLine("Process counts: 10, 20, 30, 40, 50");
Console.WriteLine("Trials per process count: 5");
Console.WriteLine("Algorithms: FCFS, SRTF, Round Robin");
Console.WriteLine();

int completedRuns = 0;
const int expectedRuns = 5 * 5 * 3;

foreach (int processCount in processCounts)
{
    foreach (int trial in trials)
    {
        int seed = CalculateSeed(processCount, trial);

        // Generate exactly once. The same workload object is passed to every
        // scheduler. Each scheduler clones internally before mutating state.
        IReadOnlyList<ProcessModel> workload =
            workloadGenerator.Generate(processCount, seed);

        int roundRobinQuantum =
            CalculateRoundRobinQuantum(seed, minQuantum, maxQuantum);

        Console.WriteLine(
            $"Processes={processCount}, Trial={trial}, Seed={seed}, RR Quantum={roundRobinQuantum}");

        foreach (string algorithm in algorithms)
        {
            int? quantum =
                algorithm == "Round Robin"
                    ? roundRobinQuantum
                    : null;

            SimulationResult result =
                schedulerService.Run(algorithm, workload, quantum);

            completedRuns++;

            Console.WriteLine(
                $"  {result.Algorithm,-11} " +
                $"Avg Waiting={result.AverageWaitingTime,7:F2}  " +
                $"Avg Turnaround={result.AverageTurnaroundTime,7:F2}  " +
                $"Avg Response={result.AverageResponseTime,7:F2}  " +
                $"Makespan={result.Makespan}");
        }

        Console.WriteLine();
    }
}

Console.WriteLine($"Completed {completedRuns} of {expectedRuns} experiment runs.");

if (completedRuns != expectedRuns)
{
    throw new InvalidOperationException(
        $"Expected {expectedRuns} runs but completed {completedRuns}.");
}

static int CalculateSeed(int processCount, int trialNumber)
    => (processCount * 1000) + trialNumber;

static int CalculateRoundRobinQuantum(
    int seed,
    int minimum,
    int maximum)
{
    var random = new Random(seed ^ unchecked((int)0x5F3759DF));
    return random.Next(minimum, maximum + 1);
}
