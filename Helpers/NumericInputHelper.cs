using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StockPortalApp.Helpers
{
    public static class NumericInputHelper
    {
        private static readonly Regex _digitsOnly = new Regex("[^0-9]+");
        private static readonly Regex _decimalOnly = new Regex("[^0-9.]+");

        public static void AttachDigitsOnly(TextBox textBox)
        {
            if (textBox == null) return;
            textBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = _digitsOnly.IsMatch(e.Text);
            };
            DataObject.AddPastingHandler(textBox, OnPasteDigitsOnly);
        }

        public static void AttachDecimalOnly(TextBox textBox)
        {
            if (textBox == null) return;
            textBox.PreviewTextInput += (s, e) =>
            {
                if (e.Text == "." && (s as TextBox)?.Text.Contains(".") == false)
                {
                    e.Handled = false;
                }
                else
                {
                    e.Handled = _decimalOnly.IsMatch(e.Text);
                }
            };
            DataObject.AddPastingHandler(textBox, OnPasteDecimalOnly);
        }

        private static void OnPasteDigitsOnly(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (_digitsOnly.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static void OnPasteDecimalOnly(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (_decimalOnly.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
