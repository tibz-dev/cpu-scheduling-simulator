# CPU Scheduling Simulator

A C#/.NET 8 research project for simulating and comparing CPU scheduling algorithms in an audio-and-video processing workload context.

## Scheduling Algorithms

The simulator implements:

- First Come, First Served (FCFS)
- Shortest Remaining Time First (SRTF)
- Round Robin (RR)

Priority is generated as a workload attribute but is not used as a scheduling algorithm.

## Solution Structure

- `SchedulerCore` — scheduling algorithms, process models, metrics, workload generation, and scheduler service.
- `SchedulerCore.Tests` — MSTest automated tests for the scheduling core.
- `SchedulerGui` — WPF desktop interface for generating workloads, running simulations, viewing metrics/timelines, and exporting experiment results.
- `ExperimentRunner` — console application for running the complete research experiment and exporting results.

## Requirements

- Windows for the WPF GUI
- .NET 8 SDK
- Visual Studio 2022 with .NET desktop development support, or the .NET CLI

## Build and Test

From the repository root:

```powershell
dotnet build
dotnet test
```

The current automated test suite contains 8 tests.

## Run the GUI

```powershell
dotnet run --project SchedulerGui
```

The GUI allows a user to:

1. Select a process count and trial.
2. Generate a deterministic workload.
3. Select FCFS, SRTF, or Round Robin.
4. Run the simulation.
5. View per-process waiting, turnaround, and response times.
6. View average performance metrics and the execution timeline.
7. Save the complete experiment dataset to a user-selected CSV file.

For Round Robin, the quantum field is enabled when Round Robin is selected.

## Experimental Methodology

The automated experiment uses:

| Parameter | Value |
| --- | --- |
| Process counts | 10, 20, 30, 40, 50 |
| Trials per process count | 5 |
| Algorithms | FCFS, SRTF, Round Robin |
| Total scheduling runs | 75 |
| Burst time | 1–20 |
| Arrival gap | 0–5 |
| Priority | 1–10 (generated but unused) |
| Round Robin quantum | 2–8 |
| Seed formula | `ProcessCount * 1000 + TrialNumber` |

For each process count and trial, the workload is generated once and the same workload is supplied to all three scheduling algorithms. Each scheduler clones its input internally before changing simulation state.

This ensures the algorithms are compared against identical workloads for each trial.

## Run the Full Experiment

```powershell
dotnet run --project ExperimentRunner
```

The runner performs:

```text
5 process counts × 5 trials × 3 algorithms = 75 runs
```

It writes the results to:

```text
ExperimentResults/experiment_results.csv
```

The GUI's **Save Experiment Results** button can also generate the same experiment dataset and lets the user choose the destination folder and filename through the Windows Save As dialog.

## CSV Output

The exported dataset contains:

```text
ProcessCount
Trial
Seed
Algorithm
RRQuantum
AverageWaitingTime
AverageTurnaroundTime
AverageResponseTime
Makespan
```

FCFS and SRTF leave `RRQuantum` blank. Round Robin records the quantum used for that workload.

Numeric values are written using culture-independent decimal formatting so the CSV can be loaded reliably by tools such as Python/pandas.

## Performance Metrics

For each process:

```text
Turnaround Time = Completion Time - Arrival Time
Waiting Time    = Turnaround Time - Burst Time
Response Time   = First Start Time - Arrival Time
```

The simulator reports average waiting time, average turnaround time, average response time, and makespan for each scheduling run.

## Reproducibility

Workloads are deterministic. For example:

```text
10 processes, Trial 1 -> Seed 10001
10 processes, Trial 2 -> Seed 10002
20 processes, Trial 1 -> Seed 20001
50 processes, Trial 5 -> Seed 50005
```

Running an experiment again with the same configuration reproduces the same generated workload.

## Research Workflow

The project workflow is:

```text
Workload Generation
        |
        v
FCFS / SRTF / Round Robin
        |
        v
Metric Calculation
        |
        v
75 Experimental Runs
        |
        v
CSV Export
        |
        v
Statistical Analysis and Visualization
```

The exported CSV is intended to be used for the project's subsequent Python/pandas analysis and comparison graphs.

## License

No license has currently been specified for this repository.
