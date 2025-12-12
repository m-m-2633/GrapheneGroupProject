using GrapheneSensore.ViewModels;
using System;
using System.Windows;

namespace GrapheneSensore.Views
{
    public partial class ApplicantManagementWindow : Window
    {
        public ApplicantManagementWindow(Guid userId)
        {
            InitializeComponent();
            DataContext = new ApplicantManagementViewModel(userId);
        }
    }
}
