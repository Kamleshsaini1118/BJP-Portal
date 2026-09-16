using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using StockPortalApp.Data;
using StockPortalApp.Models;

namespace StockPortalApp
{
    public partial class AddEditReminderWindow : Window
    {
        private readonly UtilityReminder? _editingReminder;

        public UtilityReminder? SavedReminder { get; private set; }

        public AddEditReminderWindow(UtilityReminder? reminderToEdit = null)
        {
            InitializeComponent();
            _editingReminder = reminderToEdit;

            PopulateDropdowns();

            if (_editingReminder != null)
            {
                HeaderText.Text = "Edit Reminder";
                SubmitButton.Content = "Save Changes";
                LoadReminderData(_editingReminder);
            }
            else
            {
                HeaderText.Text = "Create Reminder";
                SubmitButton.Content = "Submit";
            }
        }

        private void PopulateDropdowns()
        {
            // Utility options
            var utilities = new List<string>
            {
                "Select utility",
                "Electricity",
                "Internet",
                "Water",
                "Office Rent",
                "Telephone / Mobile",
                "Property Tax",
                "Insurance",
                "Gas",
                "Generator Fuel",
                "Other"
            };
            UtilityCombo.ItemsSource = utilities;
            UtilityCombo.SelectedIndex = 0;

            // Day of month options (1st to 31st)
            var dayOptions = new List<string> { "Select day of month" };
            for (int i = 1; i <= 31; i++)
            {
                dayOptions.Add(UtilityReminder.FormatDayOfMonth(i));
            }
            DueDayCombo.ItemsSource = dayOptions;
            DueDayCombo.SelectedIndex = 0;
        }

        private void LoadReminderData(UtilityReminder reminder)
        {
            // Select utility
            if (!string.IsNullOrEmpty(reminder.UtilityName))
            {
                UtilityCombo.SelectedItem = reminder.UtilityName;
            }

            // Select due day
            if (reminder.DueDay >= 1 && reminder.DueDay <= 31)
            {
                DueDayCombo.SelectedIndex = reminder.DueDay;
            }

            // Check pill checkboxes
            if (reminder.RemindDaysBefore != null)
            {
                ChkN1.IsChecked = reminder.RemindDaysBefore.Contains(1);
                ChkN2.IsChecked = reminder.RemindDaysBefore.Contains(2);
                ChkN3.IsChecked = reminder.RemindDaysBefore.Contains(3);
                ChkN5.IsChecked = reminder.RemindDaysBefore.Contains(5);
                ChkN7.IsChecked = reminder.RemindDaysBefore.Contains(7);
                ChkN10.IsChecked = reminder.RemindDaysBefore.Contains(10);
                ChkN15.IsChecked = reminder.RemindDaysBefore.Contains(15);
            }

            // Remarks
            RemarksBox.Text = reminder.Remarks ?? string.Empty;
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            string selectedUtility = UtilityCombo.SelectedItem as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(selectedUtility) || selectedUtility == "Select utility")
            {
                MessageBox.Show("Please select a utility type.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                UtilityCombo.Focus();
                return;
            }

            if (DueDayCombo.SelectedIndex <= 0)
            {
                MessageBox.Show("Please select the due day of the month.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                DueDayCombo.Focus();
                return;
            }

            int dueDay = DueDayCombo.SelectedIndex; // 1-indexed matching 1st to 31st

            // Collect checked lead days
            var selectedDays = new List<int>();
            if (ChkN1.IsChecked == true) selectedDays.Add(1);
            if (ChkN2.IsChecked == true) selectedDays.Add(2);
            if (ChkN3.IsChecked == true) selectedDays.Add(3);
            if (ChkN5.IsChecked == true) selectedDays.Add(5);
            if (ChkN7.IsChecked == true) selectedDays.Add(7);
            if (ChkN10.IsChecked == true) selectedDays.Add(10);
            if (ChkN15.IsChecked == true) selectedDays.Add(15);

            if (selectedDays.Count == 0)
            {
                MessageBox.Show("Please select at least one 'Remind Me Before' option.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reminder = _editingReminder ?? new UtilityReminder();
            reminder.UtilityName = selectedUtility;
            reminder.DueDay = dueDay;
            reminder.RemindDaysBefore = selectedDays;
            reminder.Remarks = string.IsNullOrWhiteSpace(RemarksBox.Text) ? null : RemarksBox.Text.Trim();

            await UtilityReminderRepository.SaveReminderAsync(reminder);
            SavedReminder = reminder;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}