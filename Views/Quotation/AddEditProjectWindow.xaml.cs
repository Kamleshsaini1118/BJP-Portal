using System;
using System.Windows;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp
{
    public partial class AddEditProjectWindow : Window
    {
        public QuotationProject Project { get; private set; }
        public bool IsEditMode { get; }

        public AddEditProjectWindow(QuotationProject? projectToEdit = null)
        {
            InitializeComponent();
            IsEditMode = projectToEdit != null;
            Project = projectToEdit ?? new QuotationProject();

            CategoryCombo.ItemsSource = new[]
            {
                "Select Category", "Construction", "Event", "IT & Software", "Printing & Publicity", "Transport", "Catering", "Other"
            };

            StatusCombo.ItemsSource = new[]
            {
                "Open", "Finalized", "Cancelled"
            };

            if (IsEditMode)
            {
                HeaderText.Text = "Edit Project";
                SubmitButton.Content = "Save Changes";

                ProjectNameBox.Text = Project.ProjectName;
                ClientDepartmentBox.Text = Project.ClientDepartment;
                CategoryCombo.SelectedItem = Project.Category;
                BudgetBox.Text = Project.EstimatedBudget.HasValue && Project.EstimatedBudget.Value > 0 ? Project.EstimatedBudget.Value.ToString("0") : "";
                StartDatePicker.SelectedDate = Project.DateCreated;
                StatusCombo.SelectedItem = Project.Status;
                RemarksBox.Text = Project.Remarks;
            }
            else
            {
                HeaderText.Text = "Create Project";
                SubmitButton.Content = "Submit";

                CategoryCombo.SelectedIndex = 0;
                StatusCombo.SelectedIndex = 0;
                StartDatePicker.SelectedDate = IndiaTime.Today;
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string name = ProjectNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a Project Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameBox.Focus();
                return;
            }

            string client = ClientDepartmentBox.Text.Trim();
            string category = CategoryCombo.SelectedItem?.ToString() ?? "Construction";
            string status = StatusCombo.SelectedItem?.ToString() ?? "Open";
            string remarks = RemarksBox.Text.Trim();

            decimal budget = 0m;
            string rawBudget = BudgetBox.Text ?? "";
            if (!string.IsNullOrWhiteSpace(rawBudget))
            {
                string cleanVal = rawBudget.Replace("₹", "").Replace(",", "").Trim();
                if (!decimal.TryParse(cleanVal, out budget) || budget < 0)
                {
                    MessageBox.Show("Please enter a valid numeric value for Estimated Budget.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BudgetBox.Focus();
                    return;
                }
            }

            Project.ProjectName = name;
            Project.ClientDepartment = client;
            Project.Category = category;
            Project.EstimatedBudget = budget > 0 ? budget : null;
            Project.DateCreated = StartDatePicker.SelectedDate ?? IndiaTime.Today;
            Project.Status = status;
            Project.Remarks = remarks;

            try
            {
                await QuotationRepository.SaveProjectAsync(Project);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
