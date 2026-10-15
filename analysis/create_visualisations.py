"""Create the three required report-ready graphs and comparison tables.

Run from the repository root:
    python analysis/create_visualisations.py

Input:
    results/summary_results.csv
Output:
    results/graphs/*.png
    results/comparison_tables/*.csv

Expected input columns are the wide summary columns produced by analyse_results.py.
"""
from pathlib import Path
import pandas as pd
import matplotlib.pyplot as plt

ROOT = Path(__file__).resolve().parents[1]
INPUT = ROOT / "results" / "summary_results.csv"
GRAPHS = ROOT / "results" / "graphs"
TABLES = ROOT / "results" / "comparison_tables"
ALGORITHMS = ["FCFS", "SRTF", "Round Robin"]
METRICS = {
    "AverageWaitingTime": ("AverageWaitingTime_Mean", "AverageWaitingTime_SD", "Average Waiting Time"),
    "AverageTurnaroundTime": ("AverageTurnaroundTime_Mean", "AverageTurnaroundTime_SD", "Average Turnaround Time"),
    "AverageResponseTime": ("AverageResponseTime_Mean", "AverageResponseTime_SD", "Average Response Time"),
    "Makespan": ("Makespan_Mean", "Makespan_SD", "Makespan"),
}

def main():
    if not INPUT.exists():
        raise FileNotFoundError(f"Cannot find {INPUT}. Run analysis/analyse_results.py first.")
    df = pd.read_csv(INPUT)
    required = {"ProcessCount", "Algorithm"} | {c for pair in METRICS.values() for c in pair[:2]}
    missing = required - set(df.columns)
    if missing:
        raise ValueError(f"summary_results.csv is missing columns: {sorted(missing)}")
    if set(df["Algorithm"].unique()) != set(ALGORITHMS):
        raise ValueError("Expected algorithms: FCFS, SRTF and Round Robin.")
    GRAPHS.mkdir(parents=True, exist_ok=True)
    TABLES.mkdir(parents=True, exist_ok=True)

    for metric, (mean_col, sd_col, label) in list(METRICS.items())[:3]:
        fig, ax = plt.subplots(figsize=(8.5, 5.2))
        for algorithm in ALGORITHMS:
            part = df[df["Algorithm"] == algorithm].sort_values("ProcessCount")
            ax.plot(part["ProcessCount"], part[mean_col], marker="o", linewidth=2, label=algorithm)
        ax.set_title(f"{label} vs Process Count")
        ax.set_xlabel("Process Count")
        ax.set_ylabel(f"{label} (time units)")
        ax.set_xticks(sorted(df["ProcessCount"].unique()))
        ax.grid(True, alpha=0.3)
        ax.legend()
        fig.tight_layout()
        fig.savefig(GRAPHS / f"{metric.lower()}_vs_process_count.png", dpi=300, bbox_inches="tight")
        plt.close(fig)

    for metric, (mean_col, sd_col, label) in METRICS.items():
        table = df.pivot(index="ProcessCount", columns="Algorithm", values=mean_col)
        table = table.reindex(columns=ALGORITHMS)
        table.columns = [f"{a}_Mean" for a in table.columns]
        for algorithm in ALGORITHMS:
            sd = df.pivot(index="ProcessCount", columns="Algorithm", values=sd_col)[algorithm]
            table[f"{algorithm}_SD"] = sd
        table.reset_index().to_csv(TABLES / f"{metric}_comparison.csv", index=False, float_format="%.4f")

        compact = df[["ProcessCount", "Algorithm", mean_col, sd_col]].copy()
        compact["Mean ± SD"] = compact.apply(lambda r: f'{r[mean_col]:.2f} ± {r[sd_col]:.2f}', axis=1)
        compact[["ProcessCount", "Algorithm", "Mean ± SD"]].to_csv(
            TABLES / f"{metric}_mean_plus_sd.csv", index=False
        )
    print("Created 3 graphs and comparison tables under results/.")

if __name__ == "__main__":
    main()
