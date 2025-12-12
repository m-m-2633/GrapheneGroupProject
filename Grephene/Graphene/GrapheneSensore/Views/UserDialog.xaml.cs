using GrapheneSensore.Models;
using GrapheneSensore.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace GrapheneSensore.Views
{
    public partial class UserDialog : Window
    {
        private readonly UserService _userService;
        private readonly User? _editingUser;
        private readonly bool _isEditMode;

        public UserDialog(UserService userService, User? user = null)
        {
            InitializeComponent();
            _userService = userService;
            _editingUser = user;
            _isEditMode = user != null;

            Loaded += UserDialog_Loaded;
            UserTypeComboBox.SelectionChanged += UserTypeComboBox_SelectionChanged;
        }

        private async void UserDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var clinicians = await _userService.GetCliniciansAsync();
            ClinicianComboBox.ItemsSource = clinicians;

            if (_isEditMode && _editingUser != null)
            {
                TitleText.Text = "Edit User";
                PasswordPanel.Visibility = Visibility.Collapsed;
                UsernameTextBox.Text = _editingUser.Username;
                UsernameTextBox.IsEnabled = false;
                
                UserTypeComboBox.SelectedIndex = _editingUser.UserType switch
                {
                    "Patient" => 0,
                    "Clinician" => 1,
                    "Admin" => 2,
                    _ => 0
                };

                FirstNameTextBox.Text = _editingUser.FirstName;
                LastNameTextBox.Text = _editingUser.LastName;
                EmailTextBox.Text = _editingUser.Email;
                PhoneTextBox.Text = _editingUser.PhoneNumber;
                IsActiveCheckBox.IsChecked = _editingUser.IsActive;

                if (_editingUser.AssignedClinicianId.HasValue)
                {
                    ClinicianComboBox.SelectedValue = clinicians.FirstOrDefault(c => c.UserId == _editingUser.AssignedClinicianId);
                }
            }
        }

        private void UserTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (UserTypeComboBox.SelectedIndex == 0)
            {
                ClinicianPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ClinicianPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageText.Text = string.Empty;
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                ErrorMessageText.Text = "Please fill in all required fields.";
                return;
            }

            if (!_isEditMode && string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ErrorMessageText.Text = "Password is required for new users.";
                return;
            }

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                string userType = UserTypeComboBox.SelectedIndex switch
                {
                    0 => "Patient",
                    1 => "Clinician",
                    2 => "Admin",
                    _ => "Patient"
                };

                Guid? assignedClinicianId = null;
                if (userType == "Patient" && ClinicianComboBox.SelectedItem is User selectedClinician)
                {
                    assignedClinicianId = selectedClinician.UserId;
                }

                if (_isEditMode && _editingUser != null)
                {
                    _editingUser.FirstName = FirstNameTextBox.Text;
                    _editingUser.LastName = LastNameTextBox.Text;
                    _editingUser.Email = EmailTextBox.Text;
                    _editingUser.PhoneNumber = PhoneTextBox.Text;
                    _editingUser.IsActive = IsActiveCheckBox.IsChecked ?? true;
                    _editingUser.AssignedClinicianId = assignedClinicianId;

                    var (success, message) = await _userService.UpdateUserAsync(_editingUser);

                    if (success)
                    {
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        ErrorMessageText.Text = message;
                    }
                }
                else
                {
                    var (success, message, user) = await _userService.CreateUserAsync(
                        UsernameTextBox.Text,
                        PasswordBox.Password,
                        userType,
                        FirstNameTextBox.Text,
                        LastNameTextBox.Text,
                        EmailTextBox.Text,
                        PhoneTextBox.Text,
                        assignedClinicianId);

                    if (success)
                    {
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        ErrorMessageText.Text = message;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
