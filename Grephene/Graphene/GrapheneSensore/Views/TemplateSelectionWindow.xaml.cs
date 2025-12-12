using GrapheneSensore.Models;
using GrapheneSensore.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace GrapheneSensore.Views
{
    public partial class TemplateSelectionWindow : Window
    {
        private readonly TemplateSelectionViewModel _viewModel;

        public TemplateSelectionWindow(Guid userId, Guid applicantId)
        {
            InitializeComponent();
            
            _viewModel = new TemplateSelectionViewModel(userId, applicantId);
            _viewModel.TemplateSelected += ViewModel_TemplateSelected;
            DataContext = _viewModel;
        }

        private void ViewModel_TemplateSelected(object? sender, Guid sessionId)
        {
            var feedbackWindow = new FeedbackFormWindow(sessionId);
            feedbackWindow.ShowDialog();
            Close();
        }

        private void TemplateCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Template template)
            {
                _viewModel.SelectedTemplate = template;
                _viewModel.SelectTemplateCommand.Execute(null);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
