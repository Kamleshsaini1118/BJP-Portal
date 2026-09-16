using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using StockPortalApp.Data;
using StockPortalApp.Models;

namespace StockPortalApp
{
    public partial class ProjectQuotationsWindow : Window
    {
        private readonly int _projectId;
        private QuotationProject? _project;

        public ProjectQuotationsWindow(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            Loaded += ProjectQuotationsWindow_Loaded;
        }

        private async void ProjectQuotationsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshDataAsync();
        }

        private async System.Threading.Tasks.Task RefreshDataAsync()
        {
            try
            {
                _project = await QuotationRepository.GetProjectByIdAsync(_projectId);
                if (_project == null)
                {
                    MessageBox.Show("Project not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                ProjectHeaderTitle.Text = $"{_project.ProjectName} — Quotations";
                SumClientText.Text = _project.ClientDepartment;
                SumCategoryText.Text = _project.Category;
                SumBudgetText.Text = _project.EstimatedBudgetDisplay;
                SumStatusText.Text = _project.Status;
                SumStatusText.Foreground = _project.StatusFgBrush;

                var quotations = await QuotationRepository.GetQuotationsForProjectAsync(_projectId);
                QuotationsItemsControl.ItemsSource = quotations;

                EmptyQuotationsText.Visibility = quotations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading quotations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ItemStatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.Tag is int quotationId)
            {
                string newStatus = "";
                if (combo.SelectedItem is ComboBoxItem item)
                {
                    newStatus = item.Content?.ToString() ?? "";
                }
                else if (combo.SelectedValue != null)
                {
                    newStatus = combo.SelectedValue.ToString() ?? "";
                }

                if (!string.IsNullOrWhiteSpace(newStatus))
                {
                    await QuotationRepository.UpdateQuotationStatusAsync(quotationId, newStatus);
                }
            }
        }

        private async void AddQuotationButton_Click(object sender, RoutedEventArgs e)
        {
            var uploadWin = new UploadQuotationWindow(_projectId)
            {
                Owner = this
            };

            if (uploadWin.ShowDialog() == true)
            {
                await RefreshDataAsync();
            }
        }

        private async void EditQuotationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int quotationId)
            {
                var list = await QuotationRepository.GetQuotationsForProjectAsync(_projectId);
                var quotationToEdit = list.FirstOrDefault(q => q.Id == quotationId);
                if (quotationToEdit != null)
                {
                    var editWin = new UploadQuotationWindow(_projectId, quotationToEdit)
                    {
                        Owner = this
                    };
                    if (editWin.ShowDialog() == true)
                    {
                        await RefreshDataAsync();
                    }
                }
            }
        }

        private async void DeleteQuotationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int quotationId)
            {
                var res = MessageBox.Show("Are you sure you want to delete this quotation?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    await QuotationRepository.DeleteQuotationAsync(quotationId);
                    await RefreshDataAsync();
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
