using GrapheneSensore.Models;
using GrapheneSensore.Services;
using GrapheneSensore.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GrapheneSensore.ViewModels
{
    public class TemplateSelectionViewModel : ViewModelBase
    {
        private readonly TemplateService _templateService;
        private readonly Guid _userId;
        private readonly Guid _applicantId;

        private ObservableCollection<Template> _templates;
        private Template? _selectedTemplate;
        private bool _isLoading;
        private Applicant? _applicant;

        public ObservableCollection<Template> Templates
        {
            get => _templates;
            set { _templates = value; OnPropertyChanged(); }
        }

        public Template? SelectedTemplate
        {
            get => _selectedTemplate;
            set { _selectedTemplate = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public Applicant? Applicant
        {
            get => _applicant;
            set { _applicant = value; OnPropertyChanged(); }
        }

        public ICommand SelectTemplateCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler<Guid>? TemplateSelected;

        public TemplateSelectionViewModel(Guid userId, Guid applicantId)
        {
            _templateService = new TemplateService();
            _userId = userId;
            _applicantId = applicantId;
            _templates = new ObservableCollection<Template>();

            SelectTemplateCommand = new RelayCommand(_ => SelectTemplate(), _ => SelectedTemplate != null);
            CancelCommand = new RelayCommand(_ => Cancel());

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                var applicantService = new ApplicantService();
                Applicant = await applicantService.GetApplicantByIdAsync(_applicantId);
                var templates = await _templateService.GetAllTemplatesAsync();
                
                Templates.Clear();
                foreach (var template in templates)
                {
                    Templates.Add(template);
                }

                Logger.Instance.LogInfo($"Loaded {templates.Count} templates", "TemplateSelectionVM");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error loading templates", ex, "TemplateSelectionVM");
                MessageBox.Show("Failed to load templates. Please try again.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void SelectTemplate()
        {
            if (SelectedTemplate == null) return;

            try
            {
                IsLoading = true;
                var feedbackService = new FeedbackService();
                var result = await feedbackService.StartFeedbackSessionAsync(_userId, _applicantId, SelectedTemplate.TemplateId);

                if (result.success && result.session != null)
                {
                    Logger.Instance.LogInfo($"Feedback session started: {result.session.SessionId}", "TemplateSelectionVM");
                    TemplateSelected?.Invoke(this, result.session.SessionId);
                }
                else
                {
                    MessageBox.Show(result.message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error starting feedback session", ex, "TemplateSelectionVM");
                MessageBox.Show("Failed to start feedback session.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
        }
    }
}
