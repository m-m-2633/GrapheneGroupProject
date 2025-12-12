using GrapheneSensore.Models;
using GrapheneSensore.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace GrapheneSensore.Views
{
    public partial class FeedbackFormWindow : Window
    {
        private readonly FeedbackFormViewModel _viewModel;

        public FeedbackFormWindow(Guid sessionId)
        {
            InitializeComponent();
            
            _viewModel = new FeedbackFormViewModel(sessionId);
            _viewModel.FeedbackCompleted += ViewModel_FeedbackCompleted;
            _viewModel.FeedbackAborted += ViewModel_FeedbackAborted;
            DataContext = _viewModel;
        }

        private void ViewModel_FeedbackCompleted(object? sender, EventArgs e)
        {
            MessageBox.Show("Feedback completed! You can now generate PDF and send emails.", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void ViewModel_FeedbackAborted(object? sender, EventArgs e)
        {
            MessageBox.Show("Feedback aborted.", "Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void ParagraphItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is FeedbackParagraph paragraph)
            {
                _viewModel.InsertParagraphCommand.Execute(paragraph);
            }
        }
    }
}
