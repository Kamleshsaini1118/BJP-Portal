using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StockPortalApp.Helpers
{
    public enum NumericInputMode
    {
        None,
        DigitsOnly,
        DecimalOnly,
        MobileNumber
    }

    public static class NumericInput
    {
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(NumericInputMode),
                typeof(NumericInput),
                new PropertyMetadata(NumericInputMode.None, OnModeChanged));

        public static NumericInputMode GetMode(DependencyObject obj)
        {
            return (NumericInputMode)obj.GetValue(ModeProperty);
        }

        public static void SetMode(DependencyObject obj, NumericInputMode value)
        {
            obj.SetValue(ModeProperty, value);
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox) return;

            textBox.PreviewTextInput -= TextBox_PreviewTextInput;
            textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            DataObject.RemovePastingHandler(textBox, TextBox_Pasting);

            var newMode = (NumericInputMode)e.NewValue;
            if (newMode != NumericInputMode.None)
            {
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                DataObject.AddPastingHandler(textBox, TextBox_Pasting);

                if (newMode == NumericInputMode.MobileNumber)
                {
                    textBox.MaxLength = 10;
                }
            }
        }

        public static bool IsValidMobileNumber(string? number, bool isOptional = false)
        {
            if (string.IsNullOrWhiteSpace(number)) return isOptional;
            var trimmed = number.Trim();
            return trimmed.Length == 10 && Regex.IsMatch(trimmed, @"^[0-9]\d{9}$");
        }

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            var mode = GetMode(textBox);
            if (mode == NumericInputMode.DigitsOnly)
            {
                e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
            }
            else if (mode == NumericInputMode.DecimalOnly)
            {
                if (e.Text == ".")
                {
                    e.Handled = textBox.Text.Contains(".");
                }
                else
                {
                    e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
                }
            }
            else if (mode == NumericInputMode.MobileNumber)
            {
                if (textBox.SelectionStart == 0)
                {
                    e.Handled = !Regex.IsMatch(e.Text, "^[6-9][0-9]*$");
                }
                else
                {
                    e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
                }
            }
        }

        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = ((string)e.DataObject.GetData(typeof(string))).Trim();
                var mode = GetMode(textBox);

                if (mode == NumericInputMode.DigitsOnly)
                {
                    if (!Regex.IsMatch(text, "^[0-9]+$"))
                    {
                        e.CancelCommand();
                    }
                }
                else if (mode == NumericInputMode.DecimalOnly)
                {
                    if (!Regex.IsMatch(text, @"^[0-9]+(\.[0-9]+)?$"))
                    {
                        e.CancelCommand();
                    }
                }
                else if (mode == NumericInputMode.MobileNumber)
                {
                    if (!Regex.IsMatch(text, "^[6-9][0-9]*$"))
                    {
                        e.CancelCommand();
                    }
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
