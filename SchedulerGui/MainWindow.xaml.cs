using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SchedulerCore.Models;
using SchedulerCore.Services;

namespace SchedulerGui;

public partial class MainWindow : Window
{
    private readonly WorkloadGenerator _workloadGenerator = new();
    private readonly SchedulerService _schedulerService = new();

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

        UpdateSeedDisplay();
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

        if (Workload.Count == 0)
        {
            ValidationTextBlock.Text =
                "Generate a workload before running a simulation.";
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

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        Workload.Clear();
        Results.Clear();
        Timeline.Clear();

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
