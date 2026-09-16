using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using StockPortalApp.Helpers;

namespace StockPortalApp.Models
{
    public class UtilityReminder
    {
        public int Id { get; set; }
        public string UtilityName { get; set; } = string.Empty;
        public int DueDay { get; set; } = 1; // 1 to 31
        public List<int> RemindDaysBefore { get; set; } = new(); // e.g. [1, 3, 7]
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; } = IndiaTime.Now;

        // Display Helpers
        public string DueDayDisplay => FormatDayOfMonth(DueDay);

        public string RemindBeforeDisplay
        {
            get
            {
                if (RemindDaysBefore == null || RemindDaysBefore.Count == 0)
                    return "None";
                var sorted = RemindDaysBefore.OrderBy(x => x).Select(x => $"N-{x}");
                return string.Join(", ", sorted);
            }
        }

        public string NextReminderDisplay
        {
            get
            {
                var today = IndiaTime.Today;
                var (dueDate, leadDay) = CalculateNextReminder(today);

                if (dueDate.Date == today.Date)
                {
                    return $"{dueDate:dd MMM yyyy} (Due Today)";
                }
                else if (leadDay > 0)
                {
                    return $"{dueDate.AddDays(-leadDay):dd MMM yyyy} (N-{leadDay})";
                }
                return $"{dueDate:dd MMM yyyy}";
            }
        }

        public string Status
        {
            get
            {
                var today = IndiaTime.Today;
                var (dueDate, _) = CalculateNextReminder(today);

                if (dueDate.Date == today.Date)
                    return "Due Today";
                if (dueDate.Date < today.Date)
                    return "Overdue";
                return "Upcoming";
            }
        }

        public Brush StatusBgBrush
        {
            get
            {
                return Status switch
                {
                    "Due Today" => (SolidColorBrush)new BrushConverter().ConvertFrom("#FEE2E2")!, // Light Red
                    "Overdue" => (SolidColorBrush)new BrushConverter().ConvertFrom("#FEF2F2")!,
                    _ => (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF5EC")! // Light Warm Saffron
                };
            }
        }

        public Brush StatusFgBrush
        {
            get
            {
                return Status switch
                {
                    "Due Today" => (SolidColorBrush)new BrushConverter().ConvertFrom("#DC2626")!, // Deep Red
                    "Overdue" => (SolidColorBrush)new BrushConverter().ConvertFrom("#991B1B")!,
                    _ => (SolidColorBrush)new BrushConverter().ConvertFrom("#D9531E")! // Deep Saffron
                };
            }
        }

        private (DateTime DueDate, int ClosestLeadDay) CalculateNextReminder(DateTime fromDate)
        {
            int year = fromDate.Year;
            int month = fromDate.Month;
            int maxDays = DateTime.DaysInMonth(year, month);
            int targetDay = Math.Min(DueDay, maxDays);
            DateTime dueDate = new DateTime(year, month, targetDay);

            if (dueDate < fromDate)
            {
                DateTime nextMonthDate = fromDate.AddMonths(1);
                year = nextMonthDate.Year;
                month = nextMonthDate.Month;
                maxDays = DateTime.DaysInMonth(year, month);
                targetDay = Math.Min(DueDay, maxDays);
                dueDate = new DateTime(year, month, targetDay);
            }

            int leadDay = 0;
            if (RemindDaysBefore != null && RemindDaysBefore.Count > 0)
            {
                var upcomingLeads = RemindDaysBefore
                    .Where(n => dueDate.AddDays(-n) >= fromDate)
                    .OrderBy(n => dueDate.AddDays(-n))
                    .ToList();

                if (upcomingLeads.Count > 0)
                {
                    leadDay = upcomingLeads.First();
                }
                else
                {
                    leadDay = RemindDaysBefore.Min();
                }
            }

            return (dueDate, leadDay);
        }

        public static string FormatDayOfMonth(int day)
        {
            if (day <= 0 || day > 31) return $"{day}th of every month";
            string suffix = (day % 10) switch
            {
                1 when day != 11 => "st",
                2 when day != 12 => "nd",
                3 when day != 13 => "rd",
                _ => "th"
            };
            return $"{day}{suffix} of every month";
        }
    }
}