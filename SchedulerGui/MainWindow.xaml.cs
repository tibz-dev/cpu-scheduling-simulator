using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SchedulerCore.Models;
using SchedulerCore.Services;
using System.IO;

namespace SchedulerGui;

public partial class MainWindow : Window
{
    private static readonly Brush FcfsChartBrush = new SolidColorBrush(Color.FromRgb(23, 126, 137));
    private static readonly Brush SrtfChartBrush = new SolidColorBrush(Color.FromRgb(209, 73, 91));
    private static readonly Brush RoundRobinChartBrush = new SolidColorBrush(Color.FromRgb(227, 160, 24));
    private static readonly string[] WaitingTimeAlgorithms = { "FCFS", "SRTF", "Round Robin" };
    private static readonly Brush[] ExecutionChartPalette =
    [
        new SolidColorBrush(Color.FromRgb(77, 110, 245)),
        new SolidColorBrush(Color.FromRgb(20, 184, 148)),
        new SolidColorBrush(Color.FromRgb(239, 83, 80)),
        new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        new SolidColorBrush(Color.FromRgb(112, 74, 230)),
        new SolidColorBrush(Color.FromRgb(22, 169, 190)),
        new SolidColorBrush(Color.FromRgb(219, 72, 142)),
        new SolidColorBrush(Color.FromRgb(110, 145, 59))
    ];

    private readonly WorkloadGenerator _workloadGenerator = new();
    private readonly SchedulerService _schedulerService = new();
    private readonly Dictionary<(string Algorithm, int ProcessCount, int Trial), double> _waitingTimeSamples = new();
    private readonly Dictionary<string, Polyline> _waitingTimeChartLines = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Ellipse>> _waitingTimeChartMarkers = new(StringComparer.Ordinal);
    private double _waitingTimeChartMaximumAxisValue;

    private int? _generatedProcessCount;
    private int? _generatedTrialNumber;
    private int? _generatedSeed;

    public ObservableCollection<ProcessModel> Workload { get; } = new();
    public ObservableCollection<ProcessModel> Results { get; } = new();
    public ObservableCollection<ExecutionSlice> Timeline { get; } = new();
    public IReadOnlyList<AlgorithmGuideEntry> AlgorithmGuide { get; } =
    [
        new("FCFS", "Earliest arrival first; ties use PID.",
            "Non-preemptive: runs a process to completion before choosing the next."),
        new("SRTF", "Available process with the shortest remaining burst.",
            "Preemptive: reevaluates every time unit and can switch when a shorter job arrives."),
        new("Round Robin", "Processes take turns in ready-queue order.",
            "Runs each process for up to the configured quantum, then requeues it if unfinished.")
    ];
    public IReadOnlyList<WaitingTimeSeries> WaitingTimeChartSeries { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        WaitingTimeChartSeries = BuildWaitingTimeSeries();

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
            DrawExecutionGuide();
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

            var comparisonResults = new Dictionary<string, SimulationResult>
            {
                [algorithm] = result
            };

            foreach (string comparisonAlgorithm in WaitingTimeAlgorithms)
            {
                if (comparisonAlgorithm == algorithm)
                    continue;

                int? comparisonQuantum = comparisonAlgorithm == "Round Robin"
                    ? algorithm == "Round Robin"
                        ? quantum
                        : CalculateRoundRobinQuantum(_generatedSeed.Value)
                    : null;

                comparisonResults[comparisonAlgorithm] = _schedulerService.Run(
                    comparisonAlgorithm,
                    Workload,
                    comparisonQuantum);
            }

            RecordWaitingTimes(selectedProcessCount, selectedTrial, comparisonResults);
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

            csv.AppendLine("sep=,");
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
        DrawExecutionGuide();
        _generatedProcessCount = null;
        _generatedTrialNumber = null;
        _generatedSeed = null;
        _waitingTimeSamples.Clear();
        WaitingTimeChartSeries = BuildWaitingTimeSeries();
        DrawWaitingTimeChart();

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

    private IReadOnlyList<WaitingTimeSeries> BuildWaitingTimeSeries()
    {
        int[] processCounts = { 10, 20, 30, 40, 50 };
        int[] trials = { 1, 2, 3, 4, 5 };
        foreach (int processCount in processCounts)
        {
            foreach (int trial in trials)
            {
                int seed = CalculateSeed(processCount, trial);
                IReadOnlyList<ProcessModel> workload =
                    _workloadGenerator.Generate(processCount, seed);
                int quantum = CalculateRoundRobinQuantum(seed);

                foreach (string algorithm in WaitingTimeAlgorithms)
                {
                    SimulationResult result = _schedulerService.Run(
                        algorithm,
                        workload,
                        algorithm == "Round Robin" ? quantum : null);

                    _waitingTimeSamples[(algorithm, processCount, trial)] =
                        result.AverageWaitingTime;
                }
            }
        }

        return CreateWaitingTimeChartSeries(processCounts, trials);
    }

    private IReadOnlyList<WaitingTimeSeries> CreateWaitingTimeChartSeries(
        int[] processCounts,
        int[] trials)
    {
        return
        [
            CreateSeries("FCFS", FcfsChartBrush),
            CreateSeries("SRTF", SrtfChartBrush),
            CreateSeries("Round Robin", RoundRobinChartBrush)
        ];

        WaitingTimeSeries CreateSeries(string algorithm, Brush color)
            => new(
                algorithm,
                color,
                processCounts
                    .Select(count => new WaitingTimeChartPoint(
                        count,
                        trials.Average(trial =>
                            _waitingTimeSamples[(algorithm, count, trial)])))
                    .ToList());
    }

    private void RecordWaitingTimes(
        int processCount,
        int trial,
        IReadOnlyDictionary<string, SimulationResult> comparisonResults)
    {
        foreach (WaitingTimeSeries series in WaitingTimeChartSeries)
        {
            _waitingTimeSamples[(series.Algorithm, processCount, trial)] =
                comparisonResults[series.Algorithm].AverageWaitingTime;

            int pointIndex = series.Points.FindIndex(point => point.ProcessCount == processCount);
            double average = Enumerable.Range(1, 5)
                .Average(sampleTrial =>
                    _waitingTimeSamples[(series.Algorithm, processCount, sampleTrial)]);
            series.Points[pointIndex] = new WaitingTimeChartPoint(processCount, average);
        }

        UpdateWaitingTimeChartData();
    }

    private void WaitingTimeChartCanvas_Loaded(object sender, RoutedEventArgs e)
        => DrawWaitingTimeChart();

    private void WaitingTimeChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        => DrawWaitingTimeChart();

    private void DrawWaitingTimeChart()
    {
        double width = WaitingTimeChartCanvas.ActualWidth;
        double height = WaitingTimeChartCanvas.ActualHeight;

        if (width < 120 || height < 100)
            return;

        WaitingTimeChartCanvas.Children.Clear();
        _waitingTimeChartLines.Clear();
        _waitingTimeChartMarkers.Clear();

        const double left = 58;
        const double right = 14;
        const double top = 12;
        const double bottom = 34;
        double plotWidth = width - left - right;
        double plotHeight = height - top - bottom;

        if (plotWidth <= 0 || plotHeight <= 0)
            return;

        if (!WaitingTimeChartSeries.Any(series => series.Points.Count > 0))
            return;

        double maximumValue = WaitingTimeChartSeries
            .SelectMany(series => series.Points)
            .Max(point => point.AverageWaitingTime);
        double tickSize = Math.Max(1, Math.Ceiling(maximumValue / 5));
        _waitingTimeChartMaximumAxisValue = tickSize * 5;
        int[] processCounts = { 10, 20, 30, 40, 50 };

        for (int tick = 0; tick <= 5; tick++)
        {
            double y = top + plotHeight - (tick / 5d * plotHeight);
            AddChartLine(left, y, width - right, y, "#E3E7EB", 1);
            AddChartLabel(
                (tick * tickSize).ToString("0.#"),
                0,
                y - 9,
                left - 8,
                TextAlignment.Right);
        }

        AddChartLine(left, top, left, top + plotHeight, "#87909A", 1.2);
        AddChartLine(left, top + plotHeight, width - right, top + plotHeight, "#87909A", 1.2);

        for (int index = 0; index < processCounts.Length; index++)
        {
            double x = left + index * plotWidth / (processCounts.Length - 1);
            AddChartLabel(processCounts[index].ToString(), x - 25, top + plotHeight + 7, 50, TextAlignment.Center);
        }

        foreach (WaitingTimeSeries series in WaitingTimeChartSeries)
        {
            var points = series.Points.Select(point =>
            {
                int index = Array.IndexOf(processCounts, point.ProcessCount);
                double x = left + index * plotWidth / (processCounts.Length - 1);
                double y = top + plotHeight - point.AverageWaitingTime / _waitingTimeChartMaximumAxisValue * plotHeight;
                return (Index: index, Position: new Point(x, y));
            }).ToList();

            var polyline = new Polyline
            {
                Points = new PointCollection(points.Select(point => point.Position)),
                Stroke = series.Color,
                StrokeThickness = 2.5
            };
            _waitingTimeChartLines[series.Algorithm] = polyline;
            WaitingTimeChartCanvas.Children.Add(polyline);

            var markers = new List<Ellipse>();
            foreach ((int _, Point point) in points)
            {
                var marker = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = series.Color,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(marker, point.X - marker.Width / 2);
                Canvas.SetTop(marker, point.Y - marker.Height / 2);
                WaitingTimeChartCanvas.Children.Add(marker);
                markers.Add(marker);
            }

            _waitingTimeChartMarkers[series.Algorithm] = markers;
        }
    }

    private void UpdateWaitingTimeChartData()
    {
        if (_waitingTimeChartLines.Count == 0)
            return;

        double maximumValue = WaitingTimeChartSeries
            .SelectMany(series => series.Points)
            .Max(point => point.AverageWaitingTime);
        if (maximumValue > _waitingTimeChartMaximumAxisValue)
        {
            DrawWaitingTimeChart();
            return;
        }

        double width = WaitingTimeChartCanvas.ActualWidth;
        double height = WaitingTimeChartCanvas.ActualHeight;
        const double left = 58;
        const double right = 14;
        const double top = 12;
        const double bottom = 34;
        double plotWidth = width - left - right;
        double plotHeight = height - top - bottom;
        int[] processCounts = { 10, 20, 30, 40, 50 };

        foreach (WaitingTimeSeries series in WaitingTimeChartSeries)
        {
            var points = new PointCollection();
            for (int index = 0; index < series.Points.Count; index++)
            {
                WaitingTimeChartPoint chartPoint = series.Points[index];
                double x = left + index * plotWidth / (processCounts.Length - 1);
                double y = top + plotHeight - chartPoint.AverageWaitingTime / _waitingTimeChartMaximumAxisValue * plotHeight;
                var point = new Point(x, y);
                points.Add(point);
                Ellipse marker = _waitingTimeChartMarkers[series.Algorithm][index];
                Canvas.SetLeft(marker, point.X - marker.Width / 2);
                Canvas.SetTop(marker, point.Y - marker.Height / 2);
            }

            _waitingTimeChartLines[series.Algorithm].Points = points;
        }
    }

    private void AddChartLine(double x1, double y1, double x2, double y2, string color, double thickness)
    {
        WaitingTimeChartCanvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = (Brush)new BrushConverter().ConvertFromString(color)!,
            StrokeThickness = thickness
        });
    }

    private void AddChartLabel(string text, double x, double y, double width, TextAlignment alignment)
    {
        var label = new TextBlock
        {
            Text = text,
            Width = width,
            TextAlignment = alignment,
            Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
        };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        WaitingTimeChartCanvas.Children.Add(label);
    }

    private void DisplayResults(SimulationResult result)
    {
        Results.Clear();
        Timeline.Clear();

        foreach (ProcessModel process in result.Processes.OrderBy(p => p.ProcessId))
            Results.Add(process);

        foreach (ExecutionSlice slice in result.Timeline)
            Timeline.Add(slice);

        DrawExecutionGuide();
        ExecutionDetailsTabControl.SelectedItem = ExecutionGuideTabItem;

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
        DrawExecutionGuide();

        foreach (ProcessModel process in processes.OrderBy(p => p.ProcessId))
            Workload.Add(process);

        AverageWaitingText.Text = "—";
        AverageTurnaroundText.Text = "—";
        AverageResponseText.Text = "—";
    }

    private void ExecutionGuideCanvas_Loaded(object sender, RoutedEventArgs e)
        => DrawExecutionGuide();

    private void DrawExecutionGuide()
    {
        ExecutionGuideCanvas.Children.Clear();

        if (Workload.Count == 0 || Timeline.Count == 0)
        {
            ExecutionGuideCanvas.Width = 700;
            ExecutionGuideCanvas.Height = 140;
            AddExecutionGuideLabel(
                "Run a simulation to view the process execution timeline.",
                24,
                24,
                600,
                18,
                FontWeights.Normal);
            return;
        }

        const double labelWidth = 64;
        const double headerHeight = 38;
        const double rowHeight = 32;
        const double timeCellWidth = 34;
        int makespan = Timeline.Max(slice => slice.EndTime);
        List<ProcessModel> processes = Workload
            .OrderBy(process => process.ProcessId)
            .ToList();

        ExecutionGuideCanvas.Width = labelWidth + makespan * timeCellWidth + 1;
        ExecutionGuideCanvas.Height = headerHeight + processes.Count * rowHeight + 1;

        ExecutionGuideCanvas.Children.Add(new Rectangle
        {
            Width = ExecutionGuideCanvas.Width,
            Height = ExecutionGuideCanvas.Height,
            Fill = Brushes.White
        });
        AddExecutionGuideCell(0, 0, labelWidth, headerHeight, "#EEF1F6");
        AddExecutionGuideLabel("Time →", 8, 10, labelWidth - 12, 14, FontWeights.SemiBold);

        for (int unit = 0; unit < makespan; unit++)
        {
            double x = labelWidth + unit * timeCellWidth;
            AddExecutionGuideCell(x, 0, timeCellWidth, headerHeight, "#F7F8FA");
            AddExecutionGuideLabel((unit + 1).ToString(), x, 10, timeCellWidth, 14, FontWeights.Medium, TextAlignment.Center);
        }

        for (int processIndex = 0; processIndex < processes.Count; processIndex++)
        {
            ProcessModel process = processes[processIndex];
            double y = headerHeight + processIndex * rowHeight;
            AddExecutionGuideCell(0, y, labelWidth, rowHeight, "#F7F8FA");
            AddExecutionGuideLabel($"P{process.ProcessId}", 12, y + 8, labelWidth - 16, 14, FontWeights.Medium);
            AddExecutionGuideCell(labelWidth, y, makespan * timeCellWidth, rowHeight, "#FFFFFF");

            Brush processBrush = ExecutionChartPalette[(process.ProcessId - 1) % ExecutionChartPalette.Length];
            foreach (ExecutionSlice slice in Timeline.Where(slice => slice.ProcessId == process.ProcessId))
            {
                var executionBlock = new Rectangle
                {
                    Width = slice.Duration * timeCellWidth,
                    Height = rowHeight,
                    Fill = processBrush,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(executionBlock, labelWidth + slice.StartTime * timeCellWidth);
                Canvas.SetTop(executionBlock, y);
                ExecutionGuideCanvas.Children.Add(executionBlock);
            }
        }

        for (int unit = 0; unit <= makespan; unit++)
        {
            double x = labelWidth + unit * timeCellWidth;
            AddExecutionGuideLine(x, 0, x, ExecutionGuideCanvas.Height, "#DCE2EA");
        }

        for (int row = 0; row <= processes.Count; row++)
        {
            double y = headerHeight + row * rowHeight;
            AddExecutionGuideLine(0, y, ExecutionGuideCanvas.Width, y, "#DCE2EA");
        }
    }

    private void AddExecutionGuideCell(double x, double y, double width, double height, string fill)
    {
        var cell = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = (Brush)new BrushConverter().ConvertFromString(fill)!,
            Stroke = new SolidColorBrush(Color.FromRgb(220, 226, 234)),
            StrokeThickness = 0.5
        };
        Canvas.SetLeft(cell, x);
        Canvas.SetTop(cell, y);
        ExecutionGuideCanvas.Children.Add(cell);
    }

    private void AddExecutionGuideLine(double x1, double y1, double x2, double y2, string color)
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = (Brush)new BrushConverter().ConvertFromString(color)!,
            StrokeThickness = 0.5
        };
        ExecutionGuideCanvas.Children.Add(line);
    }

    private void AddExecutionGuideLabel(
        string text,
        double x,
        double y,
        double width,
        double fontSize,
        FontWeight fontWeight,
        TextAlignment alignment = TextAlignment.Left)
    {
        var label = new TextBlock
        {
            Text = text,
            Width = width,
            FontSize = fontSize,
            FontWeight = fontWeight,
            TextAlignment = alignment,
            Foreground = new SolidColorBrush(Color.FromRgb(38, 50, 66))
        };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        ExecutionGuideCanvas.Children.Add(label);
    }

    public sealed record AlgorithmGuideEntry(
        string Algorithm,
        string SelectionRule,
        string ExecutionRule);

    public sealed record WaitingTimeSeries(
        string Algorithm,
        Brush Color,
        List<WaitingTimeChartPoint> Points);

    public sealed record WaitingTimeChartPoint(
        int ProcessCount,
        double AverageWaitingTime);
}
