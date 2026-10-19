# Results

## 1. Dataset Validation

The dataset used for the analysis contains 75 experiment records: five process-count levels (10, 20, 30, 40 and 50), five trials per level, and three scheduling algorithms (FCFS, SRTF and Round Robin). The validation script previously run locally reported that all 75 expected rows were present, all 25 process-count/trial combinations contained the three required algorithms, and the seed, Round Robin quantum and required metric checks passed. The official dataset and validation report should still be approved by Member 2 before these results are treated as final or shared as verified findings.

## 2. Descriptive Statistics

For each process count and algorithm, the mean and sample standard deviation were calculated from five trials for average waiting time, average turnaround time, average response time and makespan. The summary contains 15 algorithm/process-count groups. The accompanying `descriptive_statistics.csv` provides the results in long format, while the comparison tables provide compact, report-ready views.

## 3. Waiting-Time Results

Mean waiting time increased as process count increased for all three algorithms. Across every tested process count, SRTF had the lowest mean waiting time, FCFS had the next-lowest mean, and Round Robin had the highest mean. At 10 processes, the means were 23.78 for SRTF, 36.44 for FCFS and 47.98 for Round Robin. At 50 processes, they were 119.88, 188.66 and 243.15, respectively.

## 4. Turnaround-Time Results

Mean turnaround time also increased with process count for all three algorithms. SRTF had the lowest mean turnaround time at each process count, followed by FCFS and then Round Robin. At 10 processes, the means were 34.12 for SRTF, 46.78 for FCFS and 58.32 for Round Robin. At 50 processes, the corresponding means were 130.22, 199.00 and 253.50.

## 5. Response-Time Results

Mean response time increased as process count increased for FCFS and SRTF. Round Robin's mean response time also increased overall from 13.14 at 10 processes to 101.92 at 50 processes, although its values did not rise at every step: its mean decreased from 51.86 at 30 processes to 47.93 at 40 processes. Round Robin had the lowest mean response time at each tested process count. FCFS had the highest mean response time at each count, while SRTF was between FCFS and Round Robin.

## 6. Makespan Findings

Mean makespan increased with process count, from 103.4 time units at 10 processes to 518.2 time units at 50 processes. For a given process count, the reported makespan was the same across FCFS, SRTF and Round Robin. This is consistent with the algorithms being evaluated on the same workload for each trial; makespan therefore does not distinguish the algorithms within a given workload in this dataset.

## 7. Overall Trends

Across the tested workloads, SRTF recorded the lowest mean waiting and turnaround times at all five process counts. Round Robin recorded the lowest mean response time at all five process counts, while also recording the highest mean waiting and turnaround times. FCFS response time equalled its waiting time in the reported summary. All three algorithms showed increasing mean waiting and turnaround times as process count increased. The mean makespan also increased with process count and was identical across algorithms at each process-count level.

These statements describe the observed results only. Explanations of the differences between algorithms, including the scheduling mechanisms that may account for them, should be developed in the Discussion chapter.
