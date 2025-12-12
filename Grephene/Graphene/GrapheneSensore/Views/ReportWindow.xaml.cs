using GrapheneSensore.Models;
using GrapheneSensore.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GrapheneSensore.Views
{
    public partial class ReportWindow : Window
    {
        private readonly ReportService.MetricsReport _report;
        private readonly User _patient;

        public ReportWindow(ReportService.MetricsReport report, User patient)
        {
            InitializeComponent();
            _report = report;
            _patient = patient;

            LoadReport();
        }

        private void LoadReport()
        {
            PatientNameText.Text = _patient.FullName;
            DateRangeText.Text = $"{_report.StartDate:MMM dd, yyyy} - {_report.EndDate:MMM dd, yyyy}";
            TotalFramesText.Text = _report.TotalFrames.ToString();
            AvgPeakText.Text = _report.AvgPeakPressure.ToString("F1");
            TotalAlertsText.Text = _report.TotalAlerts.ToString();
            MaxPeakText.Text = $"{_report.MaxPeakPressure} mmHg";
            AvgContactText.Text = $"{_report.AvgContactArea:F1}%";
            int lowCount = 0, mediumCount = 0, highCount = 0;
            foreach (var metric in _report.HourlyMetrics)
            {
                if (metric.AvgPeakPressure < 100)
                    lowCount++;
                else if (metric.AvgPeakPressure <= 150)
                    mediumCount++;
                else
                    highCount++;
            }

            int total = _report.HourlyMetrics.Count;
            if (total > 0)
            {
                double lowPercent = (double)lowCount / total * 100;
                double mediumPercent = (double)mediumCount / total * 100;
                double highPercent = (double)highCount / total * 100;

                LowPressureBar.Value = lowPercent;
                LowPressureText.Text = $"{lowPercent:F0}%";

                MediumPressureBar.Value = mediumPercent;
                MediumPressureText.Text = $"{mediumPercent:F0}%";

                HighPressureBar.Value = highPercent;
                HighPressureText.Text = $"{highPercent:F0}%";
            }
            GenerateRecommendations();
        }

        private void GenerateRecommendations()
        {
            RecommendationsPanel.Children.Clear();
            if (_report.AvgPeakPressure > 150)
            {
                AddRecommendation(
                    "⚠ High Pressure Alert",
                    "Average peak pressure is significantly elevated. Consider repositioning more frequently and reviewing cushion effectiveness.",
                    Brushes.OrangeRed);
            }
            else if (_report.AvgPeakPressure > 100)
            {
                AddRecommendation(
                    "⚠ Moderate Pressure",
                    "Pressure levels are moderate. Maintain regular repositioning schedule.",
                    Brushes.Orange);
            }
            else
            {
                AddRecommendation(
                    "✓ Pressure Within Normal Range",
                    "Current pressure levels are within acceptable range. Continue current care routine.",
                    Brushes.Green);
            }
            if (_report.AvgContactArea < 50)
            {
                AddRecommendation(
                    "⚠ Low Contact Area",
                    "Contact area is below optimal level. This may indicate improper positioning or cushion fit.",
                    Brushes.Orange);
            }
            if (_report.TotalAlerts > 10)
            {
                AddRecommendation(
                    "⚠ Multiple Alerts Detected",
                    $"{_report.TotalAlerts} alerts were triggered during this period. Review alert patterns and adjust care plan accordingly.",
                    Brushes.Red);
            }
            if (_report.Comparison != null)
            {
                if (_report.Comparison.PeakPressureChange > 10)
                {
                    AddRecommendation(
                        "📈 Increasing Pressure Trend",
                        "Peak pressure has increased compared to previous period. Monitor closely.",
                        Brushes.OrangeRed);
                }
                else if (_report.Comparison.PeakPressureChange < -10)
                {
                    AddRecommendation(
                        "📉 Improving Trend",
                        "Peak pressure has decreased compared to previous period. Current interventions appear effective.",
                        Brushes.Green);
                }
            }
        }

        private void AddRecommendation(string title, string description, Brush color)
        {
            var border = new Border
            {
                BorderBrush = color,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(15, 10, 10, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush(Colors.White)
            };

            var panel = new StackPanel();

            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = color,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var descText = new TextBlock
            {
                Text = description,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("TextPrimaryBrush")
            };

            panel.Children.Add(titleText);
            panel.Children.Add(descText);
            border.Child = panel;

            RecommendationsPanel.Children.Add(border);
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(this, "Sensore Patient Report");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing report: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
