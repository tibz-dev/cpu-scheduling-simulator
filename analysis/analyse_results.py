"""Member 3: mean and sample SD over five trials per process count and algorithm."""
from pathlib import Path
import pandas as pd
from validate_dataset import validate

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'results' / 'experiment_results.csv'
RESULTS = ROOT / 'results'
METRICS = ['AverageWaitingTime', 'AverageTurnaroundTime', 'AverageResponseTime', 'Makespan']


def main():
    if not validate():
        raise SystemExit('Dataset validation failed: statistics have NOT been published.')
    print('NOTE: Member 2 must approve validation_report.txt before these statistics are shared.')
    df = pd.read_csv(SOURCE)
    grouped = df.groupby(['ProcessCount', 'Algorithm'], sort=True)[METRICS]
    counts = grouped.count()
    if not counts.eq(5).all().all():
        raise SystemExit('Each group must contain five nonmissing trial observations.')
    stats = grouped.agg(['mean', 'std'])  # pandas std: sample SD (ddof=1)
    stats.columns = [f'{metric}_{"Mean" if stat == "mean" else "SD"}' for metric, stat in stats.columns]
    stats = stats.reset_index()
    ordered = ['ProcessCount', 'Algorithm']
    for metric in METRICS:
        ordered.extend([f'{metric}_Mean', f'{metric}_SD'])
    stats = stats[ordered].round(4)
    if len(stats) != 15:
        raise SystemExit(f'Expected 15 summary rows, got {len(stats)}.')
    RESULTS.mkdir(parents=True, exist_ok=True)
    stats.to_csv(RESULTS / 'summary_results.csv', index=False, float_format='%.4f')
    # Long format: one row per process count, algorithm and metric (60 rows)
    descriptive = stats.melt(id_vars=['ProcessCount', 'Algorithm'], var_name='Statistic', value_name='Value')
    descriptive[['Metric', 'Measure']] = descriptive['Statistic'].str.rsplit('_', n=1, expand=True)
    descriptive = descriptive.pivot(index=['ProcessCount', 'Algorithm', 'Metric'], columns='Measure', values='Value').reset_index()
    descriptive.columns.name = None
    descriptive = descriptive[['ProcessCount', 'Algorithm', 'Metric', 'Mean', 'SD']]
    descriptive.to_csv(RESULTS / 'descriptive_statistics.csv', index=False, float_format='%.4f')
    print(f'Generated {len(stats)} summary rows and {len(descriptive)} descriptive-statistic rows.')
    print(stats.to_string(index=False))


if __name__ == '__main__':
    main()
