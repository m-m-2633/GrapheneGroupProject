using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GrapheneSensore.Views
{
    public partial class AdminWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly UserService _userService;
        private readonly AlertService _alertService;
        private readonly PressureDataService _dataService;

        public User? CurrentUser => _authService.CurrentUser;

        public AdminWindow(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userService = new UserService();
            _alertService = new AlertService();
            _dataService = new PressureDataService();
            if (_authService.CurrentUser == null)
            {
                MessageBox.Show("Authentication error: No user is logged in.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            DataContext = this;
            Loaded += AdminWindow_Loaded;
        }

        private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
            await LoadStatsAsync();
            await LoadAlertsAsync();
        }

        private async System.Threading.Tasks.Task LoadUsersAsync()
        {
            try
            {
                try
                {
                    await DatabaseInitializationService.InitializeDatabaseAsync();
                }
                catch (Exception initEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Database initialization warning: {initEx.Message}");
                }
                
                var users = await _userService.GetAllUsersAsync();
                
                if (users == null)
                {
                    MessageBox.Show("Failed to load users: No data returned from database.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                UsersDataGrid.ItemsSource = users;
            }
            catch (NullReferenceException ex)
            {
                var errorMessage = $"Error loading users: A null reference was encountered.\n\nDetails: {ex.Message}\n\nPlease check:\n1. Database connection\n2. User data integrity\n3. Application configuration\n4. SQL Server is running\n5. appsettings.json file exists";
                
                MessageBox.Show(errorMessage, "Database Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            catch (InvalidOperationException ex)
            {
                var errorMessage = $"Database configuration error: {ex.Message}\n\nPlease check:\n1. SQL Server is running (MUZAMIL-WORLD\\SQLEXPRESS)\n2. Database 'Grephene' exists\n3. Connection string in appsettings.json is correct\n4. appsettings.json file is in the application directory";
                
                MessageBox.Show(errorMessage, "Database Configuration Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                System.Diagnostics.Debug.WriteLine($"Configuration error: {ex}");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error loading users: {ex.Message}\n\nType: {ex.GetType().Name}\n\nPlease check:\n1. Database connection\n2. SQL Server status\n3. Application configuration";
                
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner error: {ex.InnerException.Message}";
                }
                
                MessageBox.Show(errorMessage, "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async System.Threading.Tasks.Task LoadStatsAsync()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    TotalPatientsText.Text = "Loading...";
                    TotalCliniciansText.Text = "Loading...";
                    ActiveAlertsText.Text = "Loading...";
                    TotalDataPointsText.Text = "Loading...";
                });
                int patientsCount = 0;
                int cliniciansCount = 0;
                int alertsCount = 0;
                int dataPointsCount = 0;
                using (var context = new SensoreDbContext())
                {
                    patientsCount = await context.Users
                        .Where(u => u.UserType == "Patient" && u.IsActive)
                        .CountAsync();
                }

                using (var context = new SensoreDbContext())
                {
                    cliniciansCount = await context.Users
                        .Where(u => u.UserType == "Clinician" && u.IsActive)
                        .CountAsync();
                }

                using (var context = new SensoreDbContext())
                {
                    alertsCount = await context.Alerts
                        .Where(a => !a.IsAcknowledged)
                        .CountAsync();
                }

                using (var context = new SensoreDbContext())
                {
                    dataPointsCount = await context.PressureMapData
                        .CountAsync();
                }
                Dispatcher.Invoke(() =>
                {
                    TotalPatientsText.Text = patientsCount.ToString("N0");
                    TotalCliniciansText.Text = cliniciansCount.ToString("N0");
                    ActiveAlertsText.Text = alertsCount.ToString("N0");
                    TotalDataPointsText.Text = dataPointsCount.ToString("N0");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TotalPatientsText.Text = "Error";
                    TotalCliniciansText.Text = "Error";
                    ActiveAlertsText.Text = "Error";
                    TotalDataPointsText.Text = "Error";
                    
                    MessageBox.Show($"Error loading statistics: {ex.Message}\n\nPlease check your database connection.", "Statistics Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private async System.Threading.Tasks.Task LoadAlertsAsync(bool recentOnly = true)
        {
            try
            {
                var alerts = await _alertService.GetAllAlertsAsync(unacknowledgedOnly: false);
                
                if (recentOnly)
                {
                    var recentAlerts = alerts
                        .OrderByDescending(a => a.AlertDateTime)
                        .Take(100)
                        .ToList();
                    AlertsDataGrid.ItemsSource = recentAlerts;
                }
                else
                {
                    var allAlerts = alerts
                        .OrderByDescending(a => a.AlertDateTime)
                        .ToList();
                    AlertsDataGrid.ItemsSource = allAlerts;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading alerts: {ex.Message}\n\nPlease check your database connection.", "Alerts Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void AlertFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (RecentAlertsRadio == null || AllAlertsRadio == null)
                return;
            if (AlertsDataGrid == null)
                return;

            if (RecentAlertsRadio.IsChecked == true)
            {
                await LoadAlertsAsync(recentOnly: true);
            }
            else if (AllAlertsRadio.IsChecked == true)
            {
                await LoadAlertsAsync(recentOnly: false);
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserDialog(_userService);
            if (dialog.ShowDialog() == true)
            {
                _ = LoadUsersAsync();
                _ = LoadStatsAsync();
            }
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid userId)
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    var dialog = new UserDialog(_userService, user);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadUsersAsync();
                    }
                }
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid userId)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this user?", 
                    "Confirm Delete", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var (success, message) = await _userService.DeleteUserAsync(userId);
                    
                    if (success)
                    {
                        await LoadUsersAsync();
                        await LoadStatsAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void ImportData_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid userId)
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Select Pressure Data CSV File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                        long frameCount = await _dataService.ImportCsvDataAsync(openFileDialog.FileName, userId);
                        
                        MessageBox.Show(
                            $"Successfully imported {frameCount} frames of data.", 
                            "Success", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);

                        await LoadStatsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error importing data: {ex.Message}", 
                            "Error", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private async void RefreshStats_Click(object sender, RoutedEventArgs e)
        {
            await LoadStatsAsync();
            bool recentOnly = RecentAlertsRadio?.IsChecked == true;
            await LoadAlertsAsync(recentOnly);
        }

        private async void UserTypeFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (UserTypeFilter.SelectedIndex == 0)
            {
                await LoadUsersAsync();
            }
            else
            {
                string userType = UserTypeFilter.SelectedIndex switch
                {
                    1 => "Patient",
                    2 => "Clinician",
                    3 => "Admin",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(userType))
                {
                    var users = await _userService.GetUsersByTypeAsync(userType);
                    UsersDataGrid.ItemsSource = users;
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _authService.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void OpenFeedbackSystem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser != null)
            {
                var feedbackWindow = new ApplicantManagementWindow(CurrentUser.UserId);
                feedbackWindow.Show();
            }
        }

        private async void UsersDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var user = e.Row.Item as User;
                if (user != null && e.Column.Header.ToString() == "Active")
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    
                    var (success, message) = await _userService.ToggleUserActiveStatusAsync(user.UserId);
                    
                    if (!success)
                    {
                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        await LoadUsersAsync();
                    }
                    else
                    {
                        await LoadStatsAsync();
                    }
                }
            }
        }
    }
}
