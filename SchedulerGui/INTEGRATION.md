# SchedulerGui / SchedulerCore Integration

This branch contains the end-to-end implementation required for the GUI and scheduling core.

## Implemented SchedulerCore components

- `WorkloadGenerator`
- `IScheduler`
- `FcfsScheduler`
- `SrtfScheduler`
- `RoundRobinScheduler`
- `MetricsCalculator`
- `SchedulerService`
- `SimulationResult`
- `ExecutionSlice`

## Workload methodology

- Process counts supported by the GUI: 10, 20, 30, 40, 50
- Trial numbers: 1-5
- Seed: `ProcessCount * 1000 + TrialNumber`
- Burst time: 1-20 inclusive
- Arrival gap: 0-5 inclusive
- First process arrives at time 0
- Priority: generated as an unused process attribute
- All schedulers clone the supplied workload before simulation

## Scheduler behaviour

### FCFS
Non-preemptive. Processes are selected in arrival order, then ProcessId for deterministic ties.

### SRTF
Preemptive. At every time unit, the ready process with the shortest remaining time is selected. Ties use arrival time and then ProcessId.

### Round Robin
Preemptive with a configurable positive integer quantum. New arrivals join the ready queue while the current process is executing.

## Metrics

After a simulation completes:

- Turnaround Time = Completion Time - Arrival Time
- Waiting Time = Turnaround Time - Burst Time
- Response Time = First Start Time - Arrival Time

## GUI features

- process-count selector
- trial selector
- FCFS / SRTF / Round Robin selector
- Round Robin quantum validation
- deterministic seed display
- generated workload DataGrid
- process results DataGrid
- execution timeline DataGrid
- average waiting time
- average turnaround time
- average response time
- makespan shown in status
- reset and validation handling

The GUI calls `SchedulerService`; it does not contain scheduling logic.
