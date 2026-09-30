using System.Globalization;
using System.Text;
using SchedulerCore.Models;
using SchedulerCore.Services;

int[] processCounts = { 10, 20, 30, 40, 50 };
int[] trials = { 1, 2, 3, 4, 5 };
string[] algorithms = { "FCFS", "SRTF", "Round Robin" };

const int minQuantum = 2;
const int maxQuantum = 8;

var workloadGenerator = new WorkloadGenerator();
var schedulerService = new SchedulerService();
var experimentRows = new List<ExperimentRow>();

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

        // Generate exactly once so all algorithms receive the same workload.
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

            experimentRows.Add(new ExperimentRow(
                processCount,
                trial,
                seed,
                result.Algorithm,
                quantum,
                result.AverageWaitingTime,
                result.AverageTurnaroundTime,
                result.AverageResponseTime,
                result.Makespan));

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

if (completedRuns != expectedRuns)
{
    throw new InvalidOperationException(
        $"Expected {expectedRuns} runs but completed {completedRuns}.");
}

string resultsDirectory = Path.Combine(
    Directory.GetCurrentDirectory(),
    "ExperimentResults");

Directory.CreateDirectory(resultsDirectory);

string csvPath = Path.Combine(
    resultsDirectory,
    "experiment_results.csv");

WriteCsv(csvPath, experimentRows);

Console.WriteLine($"Completed {completedRuns} of {expectedRuns} experiment runs.");
Console.WriteLine($"Results exported to: {Path.GetFullPath(csvPath)}");

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

static void WriteCsv(
    string path,
    IEnumerable<ExperimentRow> rows)
{
    var csv = new StringBuilder();

    csv.AppendLine(
        "ProcessCount,Trial,Seed,Algorithm,RRQuantum," +
        "AverageWaitingTime,AverageTurnaroundTime," +
        "AverageResponseTime,Makespan");

    foreach (ExperimentRow row in rows)
    {
        csv.Append(row.ProcessCount).Append(',')
            .Append(row.Trial).Append(',')
            .Append(row.Seed).Append(',')
            .Append(EscapeCsv(row.Algorithm)).Append(',')
            .Append(row.RRQuantum?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
            .Append(row.AverageWaitingTime.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
            .Append(row.AverageTurnaroundTime.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
            .Append(row.AverageResponseTime.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
            .Append(row.Makespan)
            .AppendLine();
    }

    File.WriteAllText(path, csv.ToString(), new UTF8Encoding(false));
}

static string EscapeCsv(string value)
{
    if (!value.Contains(',') &&
        !value.Contains('"') &&
        !value.Contains('\n') &&
        !value.Contains('\r'))
    {
        return value;
    }

    return $"\"{value.Replace("\"", "\"\"")}\"";
}

internal sealed record ExperimentRow(
    int ProcessCount,
    int Trial,
    int Seed,
    string Algorithm,
    int? RRQuantum,
    double AverageWaitingTime,
    double AverageTurnaroundTime,
    double AverageResponseTime,
    int Makespan);
