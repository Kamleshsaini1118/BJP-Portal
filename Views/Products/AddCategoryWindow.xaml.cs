using StockPortalApp.Data;
using StockPortalApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace StockPortalApp
{
    public partial class AddCategoryWindow : Window
    {
        private readonly List<ProductCategory> _existingCategories;
        public ProductCategory? AddedCategory { get; private set; }

        public AddCategoryWindow(List<ProductCategory> existingCategories)
        {
            InitializeComponent();
            _existingCategories = existingCategories ?? new List<ProductCategory>();
            Loaded += (s, e) => CategoryNameBox.Focus();
        }

        private void CategoryNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var name = CategoryNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                CodePrefixBox.Text = "";
                return;
            }

            string baseLetters = new string(name.Where(char.IsLetter).ToArray()).ToUpperInvariant();
            if (baseLetters.Length > 3) baseLetters = baseLetters[..3];
            if (baseLetters.Length == 0) baseLetters = "CAT";

            string prefix = baseLetters;
            var used = new HashSet<string>(_existingCategories.Select(c => c.Prefix), StringComparer.OrdinalIgnoreCase);
            int n = 1;
            while (used.Contains(prefix))
            {
                n++;
                prefix = baseLetters[..Math.Min(2, baseLetters.Length)] + n;
            }

            CodePrefixBox.Text = prefix;
        }

        private async void AddCategorySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var name = CategoryNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryNameBox.Focus();
                return;
            }

            var prefix = CodePrefixBox.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(prefix))
            {
                MessageBox.Show("Please enter or generate a code prefix.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CodePrefixBox.Focus();
                return;
            }

            try
            {
                AddCategorySubmitButton.IsEnabled = false;
                var existingPrefixes = _existingCategories.Select(c => c.Prefix);
                AddedCategory = await ProductRepository.AddCategoryAsync(name, existingPrefixes, prefix, isActive: true);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add category.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AddCategorySubmitButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
