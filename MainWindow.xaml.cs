using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Up02Variant3;

public partial class MainWindow : Window
{
    private readonly PressureMonitor _monitor = new();
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        InitializeComponent();

        _monitor.Reset();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _timer.Tick += (_, _) => AddMeasurement();
        _timer.Start();
        Closed += (_, _) => _timer.Stop();
        RefreshDisplay();
    }

    private void AddMeasurement()
    {
        _monitor.AddMeasurement();
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        var current = _monitor.CurrentMeasurement;
        var forecast = _monitor.GetAverageForecast();
        var values = _monitor.Measurements.Select(measurement => measurement.Value).ToList();

        CurrentPressureText.Text = current.Value.ToString("F1");
        CurrentTimeText.Text = $"Обновлено в {current.Timestamp:HH:mm:ss}";
        ForecastPressureText.Text = forecast.Value.ToString("F1");
        MinimumPressureText.Text = $"{values.Min():F1} мм";
        MaximumPressureText.Text = $"{values.Max():F1} мм";
        SampleCountText.Text = $"{_monitor.Measurements.Count} измерений в графике";

        DrawChart();
    }

    private void DrawChart()
    {
        double width = PlotCanvas.ActualWidth;
        double height = PlotCanvas.ActualHeight;
        if (width < 100 || height < 100)
            return;

        PlotCanvas.Children.Clear();

        const double left = 48;
        const double right = 18;
        const double top = 18;
        const double bottom = 32;
        double plotWidth = width - left - right;
        double plotHeight = height - top - bottom;

        var measurements = _monitor.Measurements;
        var values = measurements.Select(measurement => measurement.Value).ToList();
        double minimum = Math.Floor((values.Min() - 2) / 5) * 5;
        double maximum = Math.Ceiling((values.Max() + 2) / 5) * 5;
        if (maximum <= minimum)
            maximum = minimum + 10;

        for (int tick = 0; tick <= 4; tick++)
        {
            double y = top + plotHeight * tick / 4;
            double pressure = maximum - (maximum - minimum) * tick / 4;
            AddLine(left, y, width - right, y, Color.FromRgb(230, 235, 232), 1);
            AddText(pressure.ToString("F0"), 4, y - 9, 36, 18, Color.FromRgb(120, 131, 127));
        }

        int labelCount = 5;
        for (int tick = 0; tick < labelCount; tick++)
        {
            double fraction = tick / (double)(labelCount - 1);
            double x = left + plotWidth * fraction;
            AddLine(x, top, x, top + plotHeight, Color.FromRgb(239, 242, 240), 1);
            int sampleNumber = Math.Max(1, (int)Math.Round(1 + fraction * (measurements.Count - 1)));
            AddText(sampleNumber.ToString(), x - 16, top + plotHeight + 8, 32, 18,
                Color.FromRgb(120, 131, 127), TextAlignment.Center);
        }

        Point ToPoint(int index, double pressure)
        {
            double x = left + plotWidth * index / Math.Max(1, measurements.Count - 1);
            double y = top + plotHeight * (maximum - pressure) / (maximum - minimum);
            return new Point(x, y);
        }

        var currentPoints = new PointCollection();
        var forecastPoints = new PointCollection();
        var forecasts = _monitor.GetForecastHistory();
        for (int index = 0; index < measurements.Count; index++)
        {
            currentPoints.Add(ToPoint(index, measurements[index].Value));
            forecastPoints.Add(ToPoint(index, forecasts[index].Value));
        }

        PlotCanvas.Children.Add(new Polyline
        {
            Points = forecastPoints,
            Stroke = new SolidColorBrush(Color.FromRgb(217, 130, 59)),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 5, 4 },
            StrokeLineJoin = PenLineJoin.Round
        });
        PlotCanvas.Children.Add(new Polyline
        {
            Points = currentPoints,
            Stroke = new SolidColorBrush(Color.FromRgb(25, 116, 91)),
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        });

        Point lastPoint = currentPoints[^1];
        var marker = new Ellipse
        {
            Width = 9,
            Height = 9,
            Fill = new SolidColorBrush(Color.FromRgb(25, 116, 91)),
            Stroke = Brushes.White,
            StrokeThickness = 2
        };
        Canvas.SetLeft(marker, lastPoint.X - marker.Width / 2);
        Canvas.SetTop(marker, lastPoint.Y - marker.Height / 2);
        PlotCanvas.Children.Add(marker);
    }

    private void AddLine(double x1, double y1, double x2, double y2, Color color, double thickness)
    {
        PlotCanvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = thickness
        });
    }

    private void AddText(string text, double x, double y, double width, double height,
        Color color, TextAlignment alignment = TextAlignment.Right)
    {
        var label = new TextBlock
        {
            Text = text,
            Width = width,
            Height = height,
            FontSize = 11,
            Foreground = new SolidColorBrush(color),
            TextAlignment = alignment
        };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        PlotCanvas.Children.Add(label);
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
            PauseButton.Content = "Продолжить";
            StateText.Text = "ПАУЗА";
            StateText.Foreground = new SolidColorBrush(Color.FromRgb(148, 96, 58));
            StateDot.Fill = new SolidColorBrush(Color.FromRgb(217, 130, 59));
        }
        else
        {
            _timer.Start();
            PauseButton.Content = "Пауза";
            StateText.Text = "ИЗМЕРЕНИЕ";
            StateText.Foreground = new SolidColorBrush(Color.FromRgb(25, 116, 91));
            StateDot.Fill = new SolidColorBrush(Color.FromRgb(25, 116, 91));
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _monitor.Reset();
        RefreshDisplay();
    }

    private void PlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawChart();
    }
}

public abstract class PressureMeasurement
{
    protected PressureMeasurement(DateTime timestamp, double value)
    {
        Timestamp = timestamp;
        Value = value;
    }

    public DateTime Timestamp { get; }
    public double Value { get; }
}

public sealed class CurrentPressureMeasurement : PressureMeasurement
{
    public CurrentPressureMeasurement(DateTime timestamp, double value) : base(timestamp, value)
    {
    }
}

public sealed class AverageForecastMeasurement : PressureMeasurement
{
    public AverageForecastMeasurement(DateTime timestamp, double value) : base(timestamp, value)
    {
    }
}

public sealed class PressureMonitor
{
    private const int MaximumMeasurements = 60;
    private const int ForecastWindow = 12;
    private readonly Random _random = new();
    private readonly List<CurrentPressureMeasurement> _measurements = new();

    public IReadOnlyList<CurrentPressureMeasurement> Measurements => _measurements;
    public CurrentPressureMeasurement CurrentMeasurement => _measurements[^1];

    public void Reset()
    {
        _measurements.Clear();
        double value = 752;
        DateTime firstTimestamp = DateTime.Now.AddSeconds(-47 * 1.5);

        for (int index = 0; index < 48; index++)
        {
            value = NextPressure(value);
            _measurements.Add(new CurrentPressureMeasurement(
                firstTimestamp.AddSeconds(index * 1.5), value));
        }
    }

    public void AddMeasurement()
    {
        double value = NextPressure(CurrentMeasurement.Value);
        _measurements.Add(new CurrentPressureMeasurement(DateTime.Now, value));

        if (_measurements.Count > MaximumMeasurements)
            _measurements.RemoveAt(0);
    }

    public AverageForecastMeasurement GetAverageForecast()
    {
        var recentValues = _measurements.TakeLast(ForecastWindow).Select(measurement => measurement.Value);
        return new AverageForecastMeasurement(DateTime.Now, recentValues.Average());
    }

    public IReadOnlyList<AverageForecastMeasurement> GetForecastHistory()
    {
        var forecasts = new List<AverageForecastMeasurement>(_measurements.Count);
        for (int index = 0; index < _measurements.Count; index++)
        {
            int firstIndex = Math.Max(0, index - ForecastWindow + 1);
            double average = _measurements.Skip(firstIndex).Take(index - firstIndex + 1)
                .Average(measurement => measurement.Value);
            forecasts.Add(new AverageForecastMeasurement(_measurements[index].Timestamp, average));
        }

        return forecasts;
    }

    private double NextPressure(double previousValue)
    {
        double change = (_random.NextDouble() * 2 - 1) * 0.8;
        return Math.Clamp(previousValue + change, 735, 775);
    }
}