using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using SchedulerCore.Models;
using SchedulerCore.Services;

namespace SchedulerGui;

public partial class MainWindow : Window
{
    private readonly WorkloadGenerator _workloadGenerator = new();
    private readonly SchedulerService _schedulerService = new();

    private int? _generatedProcessCount;
    private int? _generatedTrialNumber;
    private int? _generatedSeed;

    public ObservableCollection<ProcessModel> Workload { get; } = new();
    public ObservableCollection<ProcessModel> Results { get; } = new();
    public ObservableCollection<ExecutionSlice> Timeline { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        ProcessCountComboBox.ItemsSource = new[] { 10, 20, 30, 40, 50 };
        TrialComboBox.ItemsSource = new[] { 1, 2, 3, 4, 5 };
        AlgorithmComboBox.ItemsSource = new[] { "FCFS", "SRTF", "Round Robin" };

        ProcessCountComboBox.SelectedIndex = 0;
        TrialComboBox.SelectedIndex = 0;
        AlgorithmComboBox.SelectedIndex = 0;

        ProcessCountComboBox.SelectionChanged += Configuration_SelectionChanged;
        TrialComboBox.SelectionChanged += Configuration_SelectionChanged;

        UpdateSeedDisplay();
    }


    private void Configuration_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSeedDisplay();

        if (Workload.Count == 0 ||
            _generatedProcessCount is null ||
            _generatedTrialNumber is null)
        {
            return;
        }

        if (ProcessCountComboBox.SelectedItem is int processCount &&
            TrialComboBox.SelectedItem is int trialNumber &&
            (processCount != _generatedProcessCount ||
             trialNumber != _generatedTrialNumber))
        {
            Results.Clear();
            Timeline.Clear();
            AverageWaitingText.Text = "—";
            AverageTurnaroundText.Text = "—";
            AverageResponseText.Text = "—";
            ValidationTextBlock.Text =
                "Configuration changed. Generate a new workload before running.";
            StatusTextBlock.Text = "Workload regeneration required";
        }
    }

    private void AlgorithmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QuantumTextBox is null || AlgorithmComboBox.SelectedItem is null)
            return;

        QuantumTextBox.IsEnabled =
            string.Equals(
                AlgorithmComboBox.SelectedItem.ToString(),
                "Round Robin",
                StringComparison.OrdinalIgnoreCase);

        ValidationTextBlock.Text = string.Empty;
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationTextBlock.Text = string.Empty;

        if (!TryReadConfiguration(
                out int processCount,
                out int trialNumber,
                out _,
                out _))
        {
            return;
        }

        int seed = CalculateSeed(processCount, trialNumber);

        IReadOnlyList<ProcessModel> generated =
            _workloadGenerator.Generate(processCount, seed);

        DisplayWorkload(generated);
        _generatedProcessCount = processCount;
        _generatedTrialNumber = trialNumber;
        _generatedSeed = seed;
        SeedText.Text = seed.ToString();
        StatusTextBlock.Text = $"Generated {processCount} processes";
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationTextBlock.Text = string.Empty;

        if (!TryReadConfiguration(
                out _,
                out _,
                out string algorithm,
                out int? quantum))
        {
            return;
        }

        if (Workload.Count == 0 ||
            _generatedProcessCount is null ||
            _generatedTrialNumber is null ||
            _generatedSeed is null)
        {
            ValidationTextBlock.Text =
                "Generate a workload before running a simulation.";
            return;
        }

        if (ProcessCountComboBox.SelectedItem is not int selectedProcessCount ||
            TrialComboBox.SelectedItem is not int selectedTrial ||
            selectedProcessCount != _generatedProcessCount ||
            selectedTrial != _generatedTrialNumber)
        {
            ValidationTextBlock.Text =
                "The process count or trial changed. Generate a new workload before running the simulation.";
            StatusTextBlock.Text = "Workload regeneration required";
            return;
        }

        try
        {
            SimulationResult result =
                _schedulerService.Run(
                    algorithm,
                    Workload,
                    quantum);

            DisplayResults(result);
        }
        catch (Exception ex)
        {
            ValidationTextBlock.Text = ex.Message;
            StatusTextBlock.Text = "Simulation failed";
        }
    }


    private void SaveResultsButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationTextBlock.Text = string.Empty;

        var dialog = new SaveFileDialog
        {
            Title = "Save Experiment Results",
            FileName = "experiment_results.csv",
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            StatusTextBlock.Text = "Generating experiment results...";
            SaveResultsButton.IsEnabled = false;

            int[] processCounts = { 10, 20, 30, 40, 50 };
            int[] trials = { 1, 2, 3, 4, 5 };
            string[] algorithms = { "FCFS", "SRTF", "Round Robin" };
            var csv = new StringBuilder();

            csv.AppendLine(
                "ProcessCount,Trial,Seed,Algorithm,RRQuantum," +
                "AverageWaitingTime,AverageTurnaroundTime," +
                "AverageResponseTime,Makespan");

            foreach (int processCount in processCounts)
            {
                foreach (int trial in trials)
                {
                    int seed = CalculateSeed(processCount, trial);
                    IReadOnlyList<ProcessModel> workload =
                        _workloadGenerator.Generate(processCount, seed);
                    int rrQuantum = CalculateRoundRobinQuantum(seed);

                    foreach (string algorithm in algorithms)
                    {
                        int? quantum = algorithm == "Round Robin"
                            ? rrQuantum
                            : null;

                        SimulationResult result =
                            _schedulerService.Run(algorithm, workload, quantum);

                        csv.Append(processCount).Append(',')
                            .Append(trial).Append(',')
                            .Append(seed).Append(',')
                            .Append(EscapeCsv(result.Algorithm)).Append(',')
                            .Append(quantum?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
                            .Append(result.AverageWaitingTime.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                            .Append(result.AverageTurnaroundTime.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                            .Append(result.AverageResponseTime.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                            .Append(result.Makespan)
                            .AppendLine();
                    }
                }
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(false));
            StatusTextBlock.Text = "Experiment results saved";

            MessageBox.Show(
                $"75 experiment results saved successfully to:\n{dialog.FileName}",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ValidationTextBlock.Text = $"Could not save experiment results: {ex.Message}";
            StatusTextBlock.Text = "Export failed";
        }
        finally
        {
            SaveResultsButton.IsEnabled = true;
        }
    }

    private static int CalculateRoundRobinQuantum(int seed)
    {
        const int minimum = 2;
        const int maximum = 8;
        var random = new Random(seed ^ unchecked((int)0x5F3759DF));
        return random.Next(minimum, maximum + 1);
    }

    private static string EscapeCsv(string value)
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

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        Workload.Clear();
        Results.Clear();
        Timeline.Clear();
        _generatedProcessCount = null;
        _generatedTrialNumber = null;
        _generatedSeed = null;

        AverageWaitingText.Text = "—";
        AverageTurnaroundText.Text = "—";
        AverageResponseText.Text = "—";
        ValidationTextBlock.Text = string.Empty;
        StatusTextBlock.Text = "Ready";

        ProcessCountComboBox.SelectedIndex = 0;
        TrialComboBox.SelectedIndex = 0;
        AlgorithmComboBox.SelectedIndex = 0;
        QuantumTextBox.Text = "4";

        UpdateSeedDisplay();
    }

    private bool TryReadConfiguration(
        out int processCount,
        out int trialNumber,
        out string algorithm,
        out int? quantum)
    {
        processCount = 0;
        trialNumber = 0;
        algorithm = string.Empty;
        quantum = null;

        if (ProcessCountComboBox.SelectedItem is not int selectedProcessCount)
        {
            ValidationTextBlock.Text = "Select a valid process count.";
            return false;
        }

        if (TrialComboBox.SelectedItem is not int selectedTrial)
        {
            ValidationTextBlock.Text = "Select a valid trial number.";
            return false;
        }

        string? selectedAlgorithm = AlgorithmComboBox.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(selectedAlgorithm))
        {
            ValidationTextBlock.Text = "Select a scheduling algorithm.";
            return false;
        }

        if (selectedAlgorithm == "Round Robin")
        {
            if (!int.TryParse(QuantumTextBox.Text, out int parsedQuantum) ||
                parsedQuantum <= 0)
            {
                ValidationTextBlock.Text =
                    "Round Robin quantum must be a positive whole number.";
                return false;
            }

            quantum = parsedQuantum;
        }

        processCount = selectedProcessCount;
        trialNumber = selectedTrial;
        algorithm = selectedAlgorithm;

        UpdateSeedDisplay();
        return true;
    }

    private void UpdateSeedDisplay()
    {
        if (SeedText is null)
            return;

        if (ProcessCountComboBox.SelectedItem is int processCount &&
            TrialComboBox.SelectedItem is int trialNumber)
        {
            SeedText.Text =
                CalculateSeed(processCount, trialNumber).ToString();
        }
        else
        {
            SeedText.Text = "—";
        }
    }

    private static int CalculateSeed(int processCount, int trialNumber)
        => processCount * 1000 + trialNumber;

    private void DisplayResults(SimulationResult result)
    {
        Results.Clear();
        Timeline.Clear();

        foreach (ProcessModel process in result.Processes.OrderBy(p => p.ProcessId))
            Results.Add(process);

        foreach (ExecutionSlice slice in result.Timeline)
            Timeline.Add(slice);

        AverageWaitingText.Text =
            result.AverageWaitingTime.ToString("0.00");

        AverageTurnaroundText.Text =
            result.AverageTurnaroundTime.ToString("0.00");

        AverageResponseText.Text =
            result.AverageResponseTime.ToString("0.00");

        StatusTextBlock.Text =
            $"{result.Algorithm} complete • Makespan {result.Makespan}";
    }

    private void DisplayWorkload(IEnumerable<ProcessModel> processes)
    {
        Workload.Clear();
        Results.Clear();
        Timeline.Clear();

        foreach (ProcessModel process in processes.OrderBy(p => p.ProcessId))
            Workload.Add(process);

        AverageWaitingText.Text = "—";
        AverageTurnaroundText.Text = "—";
        AverageResponseText.Text = "—";
    }
}
