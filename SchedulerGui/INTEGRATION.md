# SchedulerGui Integration Contract

This branch contains the final GUI structure for Choice D (WPF GUI & Visualisation).

## What SchedulerGui already owns

- Process-count selection: 10, 20, 30, 40, 50
- Trial selection: 1-5
- Algorithm selection: FCFS, SRTF, Round Robin
- Round Robin quantum validation
- Deterministic seed display using `ProcessCount * 1000 + TrialNumber`
- Workload DataGrid
- Results DataGrid
- Average waiting / turnaround / response metric display
- Reset behaviour
- Validation and status messages
- Binding to `SchedulerCore.Models.ProcessModel`

## Required SchedulerCore integration

SchedulerGui intentionally does not implement workload generation or scheduling logic.

The remaining core implementation needs to expose equivalent functionality to the following conceptual contract:

```csharp
IReadOnlyList<ProcessModel> GenerateWorkload(int processCount, int seed);

IReadOnlyList<ProcessModel> RunFcfs(IEnumerable<ProcessModel> processes);

IReadOnlyList<ProcessModel> RunSrtf(IEnumerable<ProcessModel> processes);

IReadOnlyList<ProcessModel> RunRoundRobin(
    IEnumerable<ProcessModel> processes,
    int quantum);
```

The concrete class/method names may differ. Once the real SchedulerCore classes are merged, only the two button handlers in `MainWindow.xaml.cs` need to be connected to those APIs.

## Fair-comparison rule

Before running a scheduler, SchedulerGui should pass cloned processes:

```csharp
var copy = workload.Select(p => p.Clone()).ToList();
```

This prevents one algorithm from mutating the state used by another.

## Expected process fields

The GUI currently binds to these existing `ProcessModel` members:

- ProcessId
- ArrivalTime
- BurstTime
- Priority
- FirstStartTime
- CompletionTime
- WaitingTime
- TurnaroundTime
- ResponseTime

## Do not move into SchedulerGui

The following logic must remain in SchedulerCore:

- workload generation
- FCFS
- SRTF
- Round Robin
- scheduling metric calculation
- execution/timeline calculation

This keeps the WPF layer decoupled and prevents duplicate logic.
