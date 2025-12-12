using GrapheneSensore.Models;
using GrapheneSensore.Services;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GrapheneSensore.Views
{
    public partial class PatientWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly PressureDataService _dataService;
        private readonly CommentService _commentService;
        private readonly AlertService _alertService;
        
        private List<PressureMapData> _currentData = new();
        private int _currentFrameIndex = 0;
        private DispatcherTimer? _playbackTimer;
        private bool _isPlaying = false;

        public User? CurrentUser => _authService.CurrentUser;

        public int UnacknowledgedAlertCount { get; set; }

        public PatientWindow(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = new PressureDataService();
            _commentService = new CommentService();
            _alertService = new AlertService();

            DataContext = this;
            
            Loaded += PatientWindow_Loaded;
        }

        private async void PatientWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentUser != null)
            {
                try
                {
                    UnacknowledgedAlertCount = await _alertService.GetUnacknowledgedAlertCountAsync(CurrentUser.UserId);
                    await LoadDataAsync(1);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading patient window: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadDataAsync(int hours)
        {
            if (CurrentUser == null) return;

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                _currentData = await _dataService.GetRecentFramesAsync(CurrentUser.UserId, hours);

                if (_currentData.Any())
                {
                    TimelineSlider.Maximum = _currentData.Count - 1;
                    TimelineSlider.Value = _currentData.Count - 1;
                    _currentFrameIndex = _currentData.Count - 1;

                    UpdateDisplay();
                    UpdateCharts();
                }
                else
                {
                    MessageBox.Show("No data available for the selected time range.", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void UpdateDisplay()
        {
            if (_currentData.Count == 0 || _currentFrameIndex < 0 || _currentFrameIndex >= _currentData.Count)
                return;

            var currentFrame = _currentData[_currentFrameIndex];
            HeatMapControl.MatrixData = currentFrame.Matrix;
            PeakPressureText.Text = currentFrame.PeakPressure?.ToString() ?? "0";
            ContactAreaText.Text = currentFrame.ContactAreaPercentage?.ToString("F0") ?? "0";
            double riskIndex = CalculateRiskIndex(currentFrame.PeakPressure ?? 0, currentFrame.ContactAreaPercentage ?? 0);
            RiskIndexText.Text = riskIndex.ToString("F1");
            CurrentTimeText.Text = currentFrame.RecordedDateTime.ToString("HH:mm:ss");
            FrameCountText.Text = $"Frame {_currentFrameIndex + 1} / {_currentData.Count}";
            LoadCommentsAsync(currentFrame.DataId);
        }

        private double CalculateRiskIndex(int peakPressure, decimal contactArea)
        {
            double pressureRisk = Math.Min(peakPressure / 200.0 * 10, 10);
            double areaRisk = contactArea < 50 ? (50 - (double)contactArea) / 10 : 0;
            
            return Math.Min((pressureRisk + areaRisk) / 2, 10);
        }

        private void UpdateCharts()
        {
            if (_currentData.Count == 0)
                return;

            var peakPressureValues = new ChartValues<int>();
            var contactAreaValues = new ChartValues<decimal>();

            foreach (var frame in _currentData)
            {
                peakPressureValues.Add(frame.PeakPressure ?? 0);
                contactAreaValues.Add(frame.ContactAreaPercentage ?? 0);
            }

            PeakPressureChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Peak Pressure",
                    Values = peakPressureValues,
                    PointGeometry = null,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Stroke = System.Windows.Media.Brushes.OrangeRed
                }
            };

            ContactAreaChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Contact Area",
                    Values = contactAreaValues,
                    PointGeometry = null,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Stroke = System.Windows.Media.Brushes.DodgerBlue
                }
            };
        }

        private async void LoadCommentsAsync(long dataId)
        {
            try
            {
                var comments = await _commentService.GetCommentsForDataAsync(dataId);
                CommentsListBox.ItemsSource = comments;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading comments: {ex.Message}");
            }
        }

        private async void AddComment_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommentTextBox.Text) || CurrentUser == null)
                return;

            if (_currentFrameIndex < 0 || _currentFrameIndex >= _currentData.Count)
                return;

            var currentFrame = _currentData[_currentFrameIndex];

            var (success, message, comment) = await _commentService.AddCommentAsync(
                currentFrame.DataId,
                CurrentUser.UserId,
                CommentTextBox.Text);

            if (success)
            {
                CommentTextBox.Clear();
                LoadCommentsAsync(currentFrame.DataId);
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _currentFrameIndex = (int)TimelineSlider.Value;
            UpdateDisplay();
        }

        private void PreviousFrame_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFrameIndex > 0)
            {
                TimelineSlider.Value = _currentFrameIndex - 1;
            }
        }

        private void NextFrame_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFrameIndex < _currentData.Count - 1)
            {
                TimelineSlider.Value = _currentFrameIndex + 1;
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                StopPlayback();
            }
            else
            {
                StartPlayback();
            }
        }

        private void StartPlayback()
        {
            _isPlaying = true;
            PlayPauseButton.Content = "⏸";

            _playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _playbackTimer.Tick += PlaybackTimer_Tick;
            _playbackTimer.Start();
        }

        private void StopPlayback()
        {
            _isPlaying = false;
            PlayPauseButton.Content = "▶";
            _playbackTimer?.Stop();
        }

        private void PlaybackTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentFrameIndex < _currentData.Count - 1)
            {
                TimelineSlider.Value = _currentFrameIndex + 1;
            }
            else
            {
                StopPlayback();
            }
        }

        private async void TimeRangeChanged(object sender, RoutedEventArgs e)
        {
            if (Radio1Hour.IsChecked == true)
            {
                StartDatePicker.IsEnabled = false;
                EndDatePicker.IsEnabled = false;
                await LoadDataAsync(1);
            }
            else if (Radio6Hours.IsChecked == true)
            {
                StartDatePicker.IsEnabled = false;
                EndDatePicker.IsEnabled = false;
                await LoadDataAsync(6);
            }
            else if (Radio24Hours.IsChecked == true)
            {
                StartDatePicker.IsEnabled = false;
                EndDatePicker.IsEnabled = false;
                await LoadDataAsync(24);
            }
            else if (RadioCustom.IsChecked == true)
            {
                StartDatePicker.IsEnabled = true;
                EndDatePicker.IsEnabled = true;
            }
        }

        private async void CustomDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RadioCustom.IsChecked == true && 
                StartDatePicker.SelectedDate.HasValue && 
                EndDatePicker.SelectedDate.HasValue &&
                CurrentUser != null)
            {
                try
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                    _currentData = await _dataService.GetUserDataAsync(
                        CurrentUser.UserId,
                        StartDatePicker.SelectedDate.Value,
                        EndDatePicker.SelectedDate.Value.AddDays(1));

                    if (_currentData.Any())
                    {
                        TimelineSlider.Maximum = _currentData.Count - 1;
                        TimelineSlider.Value = _currentData.Count - 1;
                        _currentFrameIndex = _currentData.Count - 1;

                        UpdateDisplay();
                        UpdateCharts();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (Radio1Hour.IsChecked == true)
                await LoadDataAsync(1);
            else if (Radio6Hours.IsChecked == true)
                await LoadDataAsync(6);
            else if (Radio24Hours.IsChecked == true)
                await LoadDataAsync(24);
            else if (RadioCustom.IsChecked == true && 
                     StartDatePicker.SelectedDate.HasValue && 
                     EndDatePicker.SelectedDate.HasValue)
            {
                CustomDateChanged(sender, null!);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _authService.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
