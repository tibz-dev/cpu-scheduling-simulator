from pathlib import Path
import math
import pandas as pd

# --------------------------------------------------
# 1. FILE LOCATIONS
# --------------------------------------------------

# This script is in the "analysis" folder.
# The project root is its parent folder.
BASE_DIR = Path(__file__).resolve().parent.parent

CSV_FILE = BASE_DIR / "data" / "experiment_results.csv"
RESULTS_DIR = BASE_DIR / "results"

RESULTS_DIR.mkdir(parents=True, exist_ok=True)

REPORT_FILE = RESULTS_DIR / "validation_report.txt"
SUMMARY_FILE = RESULTS_DIR / "summary_results.csv"

# --------------------------------------------------
# 2. EXPECTED DATA
# --------------------------------------------------

EXPECTED_COLUMNS = [
    "ProcessCount",
    "Trial",
    "Seed",
    "Algorithm",
    "RRQuantum",
    "AverageWaitingTime",
    "AverageTurnaroundTime",
    "AverageResponseTime",
    "Makespan",
]

EXPECTED_PROCESS_COUNTS = [10, 20, 30, 40, 50]
EXPECTED_TRIALS = [1, 2, 3, 4, 5]
EXPECTED_ALGORITHMS = {"FCFS", "SRTF", "Round Robin"}

METRICS = [
    "AverageWaitingTime",
    "AverageTurnaroundTime",
    "AverageResponseTime",
    "Makespan",
]

# --------------------------------------------------
# 3. VALIDATION
# --------------------------------------------------

errors = []
warnings = []

if not CSV_FILE.exists():
    raise FileNotFoundError(
        f"Could not find the experiment CSV:\n{CSV_FILE}\n\n"
        "Place experiment_results.csv in the project's data folder."
    )

df = pd.read_csv(CSV_FILE)

# Check required columns.
missing_columns = set(EXPECTED_COLUMNS) - set(df.columns)

if missing_columns:
    errors.append(
        "Missing required columns: "
        + ", ".join(sorted(missing_columns))
    )

# Check total number of experiment rows.
if len(df) != 75:
    errors.append(
        f"Expected 75 experiment rows, but found {len(df)}."
    )

# Continue detailed checks only if required columns exist.
if not missing_columns:

    # Convert numeric columns for validation.
    numeric_columns = [
        "ProcessCount",
        "Trial",
        "Seed",
        "RRQuantum",
        *METRICS,
    ]

    for column in numeric_columns:
        df[column] = pd.to_numeric(df[column], errors="coerce")

    # Process counts.
    actual_counts = set(df["ProcessCount"].dropna().tolist())

    if actual_counts != set(EXPECTED_PROCESS_COUNTS):
        errors.append(
            f"Unexpected process counts: {sorted(actual_counts)}. "
            f"Expected {EXPECTED_PROCESS_COUNTS}."
        )

    # Trial numbers.
    actual_trials = set(df["Trial"].dropna().tolist())

    if actual_trials != set(EXPECTED_TRIALS):
        errors.append(
            f"Unexpected trial numbers: {sorted(actual_trials)}. "
            f"Expected {EXPECTED_TRIALS}."
        )

    # Algorithm names.
    actual_algorithms = set(df["Algorithm"].dropna().unique())

    if actual_algorithms != EXPECTED_ALGORITHMS:
        errors.append(
            f"Unexpected algorithm names: {sorted(actual_algorithms)}. "
            f"Expected {sorted(EXPECTED_ALGORITHMS)}."
        )

    # Missing key values.
    key_columns = ["ProcessCount", "Trial", "Seed", "Algorithm"]

    if df[key_columns].isna().any().any():
        errors.append(
            "One or more rows have missing or invalid values "
            "in ProcessCount, Trial, Seed or Algorithm."
        )

    # Duplicate experiment combinations.
    duplicate_keys = ["ProcessCount", "Trial", "Algorithm"]

    if df.duplicated(duplicate_keys).any():
        errors.append(
            "Duplicate ProcessCount + Trial + Algorithm combinations found."
        )

    # Every process count and trial should have all three algorithms.
    if not df[["ProcessCount", "Trial", "Algorithm"]].isna().any().any():
        algorithm_counts = (
            df.groupby(["ProcessCount", "Trial"])["Algorithm"]
            .nunique()
        )

        if len(algorithm_counts) != 25:
            errors.append(
                f"Expected 25 process-count/trial combinations, "
                f"but found {len(algorithm_counts)}."
            )

        if not algorithm_counts.eq(3).all():
            errors.append(
                "Some process-count/trial combinations do not contain "
                "all three algorithms."
            )

    # Validate the documented seed formula.
    seed_columns = ["ProcessCount", "Trial", "Seed"]

    if not df[seed_columns].isna().any().any():
        expected_seeds = df["ProcessCount"] * 1000 + df["Trial"]

        if not df["Seed"].eq(expected_seeds).all():
            errors.append(
                "Some seeds do not match the documented formula: "
                "Seed = ProcessCount * 1000 + Trial."
            )

    # ProcessCount, Trial and Seed should be whole numbers.
    for column in ["ProcessCount", "Trial", "Seed"]:
        values = df[column].dropna()

        if not values.map(lambda x: float(x).is_integer()).all():
            errors.append(
                f"{column} contains values that are not whole numbers."
            )

    # Validate RR quantum.
    rr_rows = df["Algorithm"].eq("Round Robin")
    other_rows = df["Algorithm"].isin(["FCFS", "SRTF"])

    rr_quantum = df["RRQuantum"]

    if rr_quantum[rr_rows].isna().any():
        errors.append(
            "One or more Round Robin rows have a missing or invalid RRQuantum."
        )

    valid_rr_quantum = rr_quantum[rr_rows].dropna()

    if not valid_rr_quantum.between(2, 8).all():
        errors.append(
            "Round Robin quantum values must be between 2 and 8."
        )

    if rr_quantum[other_rows].notna().any():
        errors.append(
            "FCFS and SRTF rows should have a blank RRQuantum."
        )

    # Validate all metrics.
    for column in METRICS:
        values = df[column]

        if values.isna().any():
            errors.append(
                f"{column} contains missing or non-numeric values."
            )
            continue

        if not values.map(math.isfinite).all():
            errors.append(
                f"{column} contains infinite or invalid values."
            )
            continue

        if (values < 0).any():
            errors.append(
                f"{column} contains negative values."
            )

    # Makespan should be positive for a non-empty workload.
    if df["Makespan"].notna().all():
        if (df["Makespan"] <= 0).any():
            errors.append(
                "Makespan should be greater than zero."
            )

    # The same workload's algorithms should have the same makespan.
    if not df[
        ["ProcessCount", "Trial", "Algorithm", "Makespan"]
    ].isna().any().any():

        makespan_counts = (
            df.groupby(["ProcessCount", "Trial"])["Makespan"]
            .nunique()
        )

        if not makespan_counts.eq(1).all():
            errors.append(
                "Algorithms for the same process count and trial "
                "have different makespans. Check workload consistency."
            )

# --------------------------------------------------
# 4. WRITE VALIDATION REPORT
# --------------------------------------------------

report_lines = [
    "CPU SCHEDULING SIMULATOR",
    "EXPERIMENT DATA VALIDATION REPORT",
    "=" * 45,
    "",
    f"CSV file: {CSV_FILE}",
    f"Rows found: {len(df)}",
    f"Expected rows: 75",
    "",
]

if errors:
    report_lines.append("VALIDATION STATUS: FAILED")
else:
    report_lines.append("VALIDATION STATUS: PASSED")

report_lines.append("")

if errors:
    report_lines.append("ERRORS:")
    for number, error in enumerate(errors, start=1):
        report_lines.append(f"{number}. {error}")
else:
    report_lines.append(
        "All implemented validation checks passed."
    )

if warnings:
    report_lines.append("")
    report_lines.append("WARNINGS:")
    for warning in warnings:
        report_lines.append(f"- {warning}")

report_lines.extend([
    "",
    "Validation checks cover required columns, row count, process counts,",
    "trials, algorithms, duplicate combinations, seed formula, RR quantum,",
    "metric validity and makespan consistency.",
    "",
    "Note: A passed report confirms these checks only; it does not prove",
    "that the scheduler implementation itself is correct.",
])

REPORT_FILE.write_text(
    "\n".join(report_lines),
    encoding="utf-8"
)

print("\n".join(report_lines))
print(f"\nValidation report saved to: {REPORT_FILE}")

# Do not calculate final statistics when validation fails.
if errors:
    print("\nFix or investigate the validation errors before continuing.")
    raise SystemExit(1)

# --------------------------------------------------
# 5. CALCULATE MEANS AND STANDARD DEVIATIONS
# --------------------------------------------------

summary = (
    df.groupby(["ProcessCount", "Algorithm"])
    .agg(
        MeanWaitingTime=("AverageWaitingTime", "mean"),
        SDWaitingTime=("AverageWaitingTime", "std"),

        MeanTurnaroundTime=("AverageTurnaroundTime", "mean"),
        SDTurnaroundTime=("AverageTurnaroundTime", "std"),

        MeanResponseTime=("AverageResponseTime", "mean"),
        SDResponseTime=("AverageResponseTime", "std"),

        MeanMakespan=("Makespan", "mean"),
        SDMakespan=("Makespan", "std"),

        TrialCount=("Trial", "nunique"),
    )
    .reset_index()
    .sort_values(["ProcessCount", "Algorithm"])
)

# Expect 5 process counts x 3 algorithms = 15 groups.
if len(summary) != 15:
    raise ValueError(
        f"Expected 15 summary groups, but found {len(summary)}."
    )

if not summary["TrialCount"].eq(5).all():
    raise ValueError(
        "At least one summary group does not contain five distinct trials."
    )

# Pandas uses sample standard deviation (ddof=1) by default.
summary.to_csv(
    SUMMARY_FILE,
    index=False,
    sep=",",
    float_format="%.4f",
    encoding="utf-8-sig"
)

# --------------------------------------------------
# 6. DISPLAY RESULTS
# --------------------------------------------------

print("\nSTATISTICAL SUMMARY")
print("=" * 45)
print(summary.to_string(index=False))

print(f"\nSummary CSV saved to: {SUMMARY_FILE}")
print("\nAnalysis completed successfully.")