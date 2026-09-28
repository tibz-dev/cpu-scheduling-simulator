using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SchedulerCore.Models;

namespace SchedulerGui;

public partial class MainWindow : Window
{
    public ObservableCollection<ProcessModel> Workload { get; } = new();
    public ObservableCollection<ProcessModel> Results { get; } = new();

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
            string.Equals(AlgorithmComboBox.SelectedItem.ToString(),
                "Round Robin",
                StringComparison.OrdinalIgnoreCase);

        ValidationTextBlock.Text = string.Empty;
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationTextBlock.Text = string.Empty;

        if (!TryReadConfiguration(out int processCount, out int trialNumber, out _, out _))
            return;

        int seed = CalculateSeed(processCount, trialNumber);
        SeedText.Text = seed.ToString();

        MessageBox.Show(
            "The GUI is ready to receive the deterministic workload from SchedulerCore. " +
            "The WorkloadGenerator is not yet present on the repository's main branch, so no duplicate generator is implemented inside SchedulerGui.",
            "Workload Generator Pending",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        StatusTextBlock.Text = "Waiting for WorkloadGenerator";
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationTextBlock.Text = string.Empty;

        if (!TryReadConfiguration(out _, out _, out string algorithm, out int? quantum))
            return;

        if (Workload.Count == 0)
        {
            ValidationTextBlock.Text = "Generate a workload before running a simulation.";
            return;
        }

        string quantumMessage =
            algorithm == "Round Robin"
                ? $" Quantum: {quantum}."
                : string.Empty;

        MessageBox.Show(
            $"Selected algorithm: {algorithm}.{quantumMessage} " +
            "The scheduler execution API is not yet present on main. " +
            "SchedulerGui is intentionally not duplicating scheduling logic.",
            "SchedulerCore Integration Pending",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        StatusTextBlock.Text = "Waiting for scheduler implementation";
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        Workload.Clear();
        Results.Clear();

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
            if (!int.TryParse(QuantumTextBox.Text, out int parsedQuantum) || parsedQuantum <= 0)
            {
                ValidationTextBlock.Text = "Round Robin quantum must be a positive whole number.";
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
            SeedText.Text = CalculateSeed(processCount, trialNumber).ToString();
        }
        else
        {
            SeedText.Text = "—";
        }
    }

    private static int CalculateSeed(int processCount, int trialNumber)
        => processCount * 1000 + trialNumber;

    private void DisplayResults(IEnumerable<ProcessModel> completedProcesses)
    {
        Results.Clear();

        foreach (ProcessModel process in completedProcesses.OrderBy(p => p.ProcessId))
            Results.Add(process);

        if (Results.Count == 0)
        {
            AverageWaitingText.Text = "—";
            AverageTurnaroundText.Text = "—";
            AverageResponseText.Text = "—";
            return;
        }

        AverageWaitingText.Text = Results.Average(p => p.WaitingTime).ToString("0.00");
        AverageTurnaroundText.Text = Results.Average(p => p.TurnaroundTime).ToString("0.00");
        AverageResponseText.Text = Results.Average(p => p.ResponseTime).ToString("0.00");

        StatusTextBlock.Text = "Simulation complete";
    }

    private void DisplayWorkload(IEnumerable<ProcessModel> processes)
    {
        Workload.Clear();

        foreach (ProcessModel process in processes.OrderBy(p => p.ProcessId))
            Workload.Add(process);

        Results.Clear();
        AverageWaitingText.Text = "—";
        AverageTurnaroundText.Text = "—";
        AverageResponseText.Text = "—";
        StatusTextBlock.Text = $"Loaded {Workload.Count} processes";
    }
}
