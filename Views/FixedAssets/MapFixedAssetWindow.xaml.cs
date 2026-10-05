using StockPortalApp.Data;
using StockPortalApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StockPortalApp.Views.FixedAssets
{
    public partial class MapFixedAssetWindow : Window
    {
        private static readonly Karyalya AddNewKaryalyaSentinel = new() { Id = -1, Name = "+ Add New Karyalya" };
        private static readonly Department AddNewDeptSentinel = new() { Id = -1, Name = "+ Add New Department / Vibhag" };

        private readonly FixedAssetRoom? _editingRoom;
        private List<Karyalya> _karyalyas = new();
        private List<Department> _departments = new();
        private List<Product> _fixedAssetProducts = new();
        private bool _suppressSelectionHandler;

        private class ItemRowControls
        {
            public Grid RowGrid { get; set; } = null!;
            public ComboBox ProductCombo { get; set; } = null!;
            public TextBox CodeBox { get; set; } = null!;
            public DatePicker DatePicker { get; set; } = null!;
        }

        private readonly List<ItemRowControls> _rowControls = new();

        public MapFixedAssetWindow(FixedAssetRoom? roomToEdit = null)
        {
            InitializeComponent();
            _editingRoom = roomToEdit;
            Loaded += MapFixedAssetWindow_Loaded;
        }

        private async void MapFixedAssetWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _fixedAssetProducts = await FixedAssetRepository.GetFixedAssetProductsListAsync();
                await ReloadKaryalyasAsync();

                if (_editingRoom != null)
                {
                    Title = "Edit Room";
                    HeaderTitleText.Text = "Edit Room";
                    SubmitButton.Content = "Save Changes";

                    _suppressSelectionHandler = true;
                    KaryalyaCombo.SelectedItem = _karyalyas.FirstOrDefault(k => k.Id == _editingRoom.KaryalyaId);
                    await ReloadDepartmentsAsync(_editingRoom.KaryalyaId);
                    DepartmentCombo.SelectedItem = _departments.FirstOrDefault(d => d.Id == _editingRoom.DepartmentId);
                    _suppressSelectionHandler = false;

                    RoomNoBox.Text = _editingRoom.RoomNo;
                    RemarksBox.Text = _editingRoom.Remarks ?? "";

                    // Load mapped items for this room
                    var items = await FixedAssetRepository.GetRoomItemsAsync(_editingRoom.RoomId);
                    if (items.Count > 0)
                    {
                        foreach (var itm in items)
                        {
                            AddItemRow(itm.ProductId, itm.ItemUniqueCode, itm.MappingDate);
                        }
                    }
                    else
                    {
                        AddItemRow();
                    }
                }
                else
                {
                    AddItemRow();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task ReloadKaryalyasAsync(int? selectKaryalyaId = null)
        {
            _karyalyas = await FixedAssetRepository.GetKaryalyasAsync();
            var list = new List<Karyalya>(_karyalyas) { AddNewKaryalyaSentinel };

            _suppressSelectionHandler = true;
            KaryalyaCombo.ItemsSource = list;

            if (selectKaryalyaId.HasValue)
            {
                KaryalyaCombo.SelectedItem = _karyalyas.FirstOrDefault(k => k.Id == selectKaryalyaId.Value);
            }
            else
            {
                KaryalyaCombo.SelectedItem = null;
                KaryalyaCombo.SelectedIndex = -1;
            }
            _suppressSelectionHandler = false;

            if (KaryalyaCombo.SelectedItem is Karyalya selected && selected.Id > 0)
            {
                await ReloadDepartmentsAsync(selected.Id);
            }
            else
            {
                _departments.Clear();
                DepartmentCombo.ItemsSource = null;
                DepartmentCombo.SelectedItem = null;
                DepartmentCombo.SelectedIndex = -1;
            }
        }

        private async System.Threading.Tasks.Task ReloadDepartmentsAsync(int karyalyaId, int? selectDeptId = null)
        {
            _departments = await FixedAssetRepository.GetDepartmentsByKaryalyaAsync(karyalyaId);
            var list = new List<Department>(_departments) { AddNewDeptSentinel };

            _suppressSelectionHandler = true;
            DepartmentCombo.ItemsSource = list;

            if (selectDeptId.HasValue)
            {
                DepartmentCombo.SelectedItem = _departments.FirstOrDefault(d => d.Id == selectDeptId.Value);
            }
            else
            {
                DepartmentCombo.SelectedItem = null;
                DepartmentCombo.SelectedIndex = -1;
            }
            _suppressSelectionHandler = false;
        }

        private async void KaryalyaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionHandler) return;

            if (KaryalyaCombo.SelectedItem is Karyalya karyalya && karyalya.Id == -1)
            {
                // "+ Add New Karyalya" picked!
                var addWin = new AddKaryalyaWindow { Owner = this };
                if (addWin.ShowDialog() == true && addWin.SavedKaryalya != null)
                {
                    await ReloadKaryalyasAsync(addWin.SavedKaryalya.Id);
                }
                else
                {
                    KaryalyaCombo.SelectedItem = null;
                    KaryalyaCombo.SelectedIndex = -1;
                }
                return;
            }

            if (KaryalyaCombo.SelectedItem is Karyalya k && k.Id > 0)
            {
                await ReloadDepartmentsAsync(k.Id);
            }
            else
            {
                DepartmentCombo.ItemsSource = null;
                DepartmentCombo.SelectedItem = null;
                DepartmentCombo.SelectedIndex = -1;
            }
        }

        private async void DepartmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionHandler) return;

            if (DepartmentCombo.SelectedItem is Department dept && dept.Id == -1)
            {
                // "+ Add New Department / Vibhag" picked!
                int currentKaryalyaId = (KaryalyaCombo.SelectedItem is Karyalya k && k.Id > 0) ? k.Id : 0;
                var addDeptWin = new AddDepartmentWindow(_karyalyas, currentKaryalyaId) { Owner = this };
                if (addDeptWin.ShowDialog() == true && addDeptWin.SavedDepartment != null)
                {
                    if (KaryalyaCombo.SelectedItem == null || ((Karyalya)KaryalyaCombo.SelectedItem).Id != addDeptWin.SavedDepartment.KaryalyaId)
                    {
                        await ReloadKaryalyasAsync(addDeptWin.SavedDepartment.KaryalyaId);
                    }
                    await ReloadDepartmentsAsync(addDeptWin.SavedDepartment.KaryalyaId, addDeptWin.SavedDepartment.Id);
                }
                else
                {
                    DepartmentCombo.SelectedItem = null;
                    DepartmentCombo.SelectedIndex = -1;
                }
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            AddItemRow();
        }

        private void AddItemRow(int? productId = null, string? uniqueCode = null, DateTime? mappingDate = null)
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });

            // 1. Product ComboBox
            var prodCombo = new ComboBox
            {
                Style = (Style)FindResource("RoundedComboBox"),
                Tag = "Select item",
                DisplayMemberPath = "Name",
                ItemsSource = _fixedAssetProducts,
                Cursor = Cursors.Hand
            };
            if (productId.HasValue)
            {
                prodCombo.SelectedItem = _fixedAssetProducts.FirstOrDefault(p => p.Id == productId.Value);
            }
            else
            {
                prodCombo.SelectedItem = null;
                prodCombo.SelectedIndex = -1;
            }
            Grid.SetColumn(prodCombo, 0);

            // 2. Item Unique Code TextBox with rounded style and placeholder
            var codeBox = new TextBox
            {
                Style = (Style)FindResource("RoundedTextBox"),
                Tag = "e.g. T-46-1",
                FontSize = 12.5,
                Text = uniqueCode ?? "",
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(codeBox, 2);

            // 3. Mapping Date
            var datePicker = new DatePicker
            {
                Style = (Style)FindResource("RoundedDatePicker"),
                SelectedDate = mappingDate ?? DateTime.Today,
                Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-IN")
            };
            Grid.SetColumn(datePicker, 4);

            // 4. Delete button [✕]
            var delBtn = new Button
            {
                Content = "✕",
                Style = (Style)FindResource("OutlineButton"),
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5")),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(delBtn, 5);

            var rowInfo = new ItemRowControls
            {
                RowGrid = rowGrid,
                ProductCombo = prodCombo,
                CodeBox = codeBox,
                DatePicker = datePicker
            };

            delBtn.Click += (s, ev) =>
            {
                if (_rowControls.Count > 1)
                {
                    ItemsContainer.Children.Remove(rowGrid);
                    _rowControls.Remove(rowInfo);
                }
                else
                {
                    prodCombo.SelectedItem = null;
                    prodCombo.SelectedIndex = -1;
                    codeBox.Text = "";
                }
            };

            // Press Enter in code box to automatically append the next item of the same product!
            codeBox.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter)
                {
                    ev.Handled = true;
                    int? curProdId = (prodCombo.SelectedItem as Product)?.Id;
                    AddItemRow(curProdId);
                    // Focus code box of newly added row
                    if (_rowControls.Count > 0)
                    {
                        _rowControls[^1].CodeBox.Focus();
                    }
                }
            };

            rowGrid.Children.Add(prodCombo);
            rowGrid.Children.Add(codeBox);
            rowGrid.Children.Add(datePicker);
            rowGrid.Children.Add(delBtn);

            _rowControls.Add(rowInfo);
            ItemsContainer.Children.Add(rowGrid);
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (KaryalyaCombo.SelectedItem is not Karyalya karyalya || karyalya.Id <= 0)
            {
                MessageBox.Show("Please select a Karyalya.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DepartmentCombo.SelectedItem is not Department dept || dept.Id <= 0)
            {
                MessageBox.Show("Please select a Department / Vibhag.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var itemsToSave = new List<FixedAssetRoomItem>();
            foreach (var row in _rowControls)
            {
                var prod = row.ProductCombo.SelectedItem as Product;
                var code = row.CodeBox.Text.Trim();
                var dt = row.DatePicker.SelectedDate ?? DateTime.Today;

                if (string.IsNullOrWhiteSpace(code)) continue;

                if (prod == null || !prod.Id.HasValue)
                {
                    MessageBox.Show("Please select an item for each entered code.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                itemsToSave.Add(new FixedAssetRoomItem
                {
                    ProductId = prod.Id.Value,
                    ProductName = prod.Name,
                    ItemUniqueCode = code,
                    MappingDate = dt,
                    Status = "Active"
                });
            }

            if (itemsToSave.Count == 0)
            {
                MessageBox.Show("Please add at least one fixed asset item code in this room.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var room = new FixedAssetRoom
            {
                RoomId = _editingRoom?.RoomId ?? 0,
                KaryalyaId = karyalya.Id,
                DepartmentId = dept.Id,
                RoomNo = RoomNoBox.Text.Trim(),
                Remarks = RemarksBox.Text.Trim()
            };

            SubmitButton.IsEnabled = false;
            try
            {
                await FixedAssetRepository.SaveRoomMappingAsync(room, itemsToSave);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save room mapping.\n\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
