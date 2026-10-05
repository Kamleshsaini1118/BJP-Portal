using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using StockPortalApp.Data;
using StockPortalApp.Models;

namespace StockPortalApp.Views.FixedAssets
{
    public partial class MoveAssetWindow : Window
    {
        private readonly FixedAssetReportItem _item;
        private List<Karyalya> _karyalyas = new();
        private List<Department> _departments = new();
        private bool _isHandlingKaryalyaChange = false;
        private bool _isHandlingDeptChange = false;

        public MoveAssetWindow(FixedAssetReportItem item)
        {
            InitializeComponent();
            _item = item;

            CurrentAssetCodeText.Text = item.ItemUniqueCode;
            CurrentAssetNameText.Text = string.IsNullOrWhiteSpace(item.ProductCode)
                ? item.ItemName
                : $"{item.ItemName} ({item.ProductCode})";
            CurrentAssetLocationText.Text = $"{item.KaryalyaName} > {item.DepartmentName}, {item.RoomNoDisplay}";

            MoveDatePicker.SelectedDate = DateTime.Today;

            Loaded += MoveAssetWindow_Loaded;
        }

        private async void MoveAssetWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadKaryalyasAsync();
        }

        private async Task LoadKaryalyasAsync(int? selectKaryalyaId = null)
        {
            try
            {
                _karyalyas = await FixedAssetRepository.GetKaryalyasAsync();
                RebuildKaryalyaCombo(selectKaryalyaId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load karyalyas.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RebuildKaryalyaCombo(int? selectKaryalyaId = null)
        {
            _isHandlingKaryalyaChange = true;
            try
            {
                var list = new List<Karyalya>(_karyalyas);
                list.Add(new Karyalya { Id = -999, Name = "+ Add New Karyalya" });

                TargetKaryalyaCombo.ItemsSource = list;

                if (selectKaryalyaId.HasValue)
                {
                    TargetKaryalyaCombo.SelectedItem = list.FirstOrDefault(k => k.Id == selectKaryalyaId.Value);
                }
                else
                {
                    // Default to current asset's karyalya if available
                    var current = list.FirstOrDefault(k => string.Equals(k.Name, _item.KaryalyaName, StringComparison.OrdinalIgnoreCase));
                    if (current != null)
                        TargetKaryalyaCombo.SelectedItem = current;
                    else
                        TargetKaryalyaCombo.SelectedIndex = -1;
                }
            }
            finally
            {
                _isHandlingKaryalyaChange = false;
            }

            if (TargetKaryalyaCombo.SelectedItem is Karyalya sel && sel.Id > 0)
            {
                _ = RebuildDepartmentComboAsync(sel.Id);
            }
        }

        private async void TargetKaryalyaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isHandlingKaryalyaChange) return;

            if (TargetKaryalyaCombo.SelectedItem is Karyalya selectedKaryalya)
            {
                if (selectedKaryalya.Id == -999)
                {
                    TargetKaryalyaCombo.SelectedItem = null;
                    await AddNewKaryalyaFlowAsync();
                    return;
                }

                await RebuildDepartmentComboAsync(selectedKaryalya.Id);
            }
            else
            {
                TargetDepartmentCombo.ItemsSource = null;
                TargetDepartmentCombo.IsEnabled = false;
            }
        }

        private async Task AddNewKaryalyaFlowAsync()
        {
            var win = new AddKaryalyaWindow { Owner = this };
            if (win.ShowDialog() == true && win.SavedKaryalya != null)
            {
                _karyalyas = await FixedAssetRepository.GetKaryalyasAsync();
                RebuildKaryalyaCombo(win.SavedKaryalya.Id);
            }
            else
            {
                RebuildKaryalyaCombo();
            }
        }

        private async Task RebuildDepartmentComboAsync(int karyalyaId, int? selectDeptId = null)
        {
            _isHandlingDeptChange = true;
            try
            {
                _departments = await FixedAssetRepository.GetDepartmentsByKaryalyaAsync(karyalyaId);
                var list = new List<Department>(_departments);
                list.Add(new Department { Id = -999, Name = "+ Add New Department", KaryalyaId = karyalyaId });

                TargetDepartmentCombo.ItemsSource = list;
                TargetDepartmentCombo.IsEnabled = true;

                if (selectDeptId.HasValue)
                {
                    TargetDepartmentCombo.SelectedItem = list.FirstOrDefault(d => d.Id == selectDeptId.Value);
                }
                else
                {
                    TargetDepartmentCombo.SelectedIndex = -1;
                }
            }
            finally
            {
                _isHandlingDeptChange = false;
            }
        }

        private async void TargetDepartmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isHandlingDeptChange) return;

            if (TargetDepartmentCombo.SelectedItem is Department selectedDept)
            {
                if (selectedDept.Id == -999)
                {
                    TargetDepartmentCombo.SelectedItem = null;
                    if (TargetKaryalyaCombo.SelectedItem is Karyalya karyalya && karyalya.Id > 0)
                    {
                        await AddNewDepartmentFlowAsync(karyalya.Id);
                    }
                    else
                    {
                        MessageBox.Show("Please select a Karyalya first.", "Select Karyalya", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TargetDepartmentCombo.SelectedIndex = -1;
                    }
                }
            }
        }

        private async Task AddNewDepartmentFlowAsync(int karyalyaId)
        {
            var win = new AddDepartmentWindow(_karyalyas, karyalyaId) { Owner = this };
            if (win.ShowDialog() == true && win.SavedDepartment != null)
            {
                _departments = await FixedAssetRepository.GetDepartmentsByKaryalyaAsync(karyalyaId);
                await RebuildDepartmentComboAsync(karyalyaId, win.SavedDepartment.Id);
            }
            else
            {
                await RebuildDepartmentComboAsync(karyalyaId);
            }
        }

        private async void SaveMoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (TargetKaryalyaCombo.SelectedItem is not Karyalya selKaryalya || selKaryalya.Id <= 0)
            {
                MessageBox.Show("Please select the target Karyalya.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TargetKaryalyaCombo.Focus();
                return;
            }

            if (TargetDepartmentCombo.SelectedItem is not Department selDept || selDept.Id <= 0)
            {
                MessageBox.Show("Please select the target Department.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TargetDepartmentCombo.Focus();
                return;
            }

            var roomNo = TargetRoomNoBox.Text.Trim();
            var moveDate = MoveDatePicker.SelectedDate ?? DateTime.Today;
            var remarks = MoveRemarksBox.Text.Trim();

            SaveMoveButton.IsEnabled = false;
            try
            {
                bool success = await FixedAssetRepository.MoveAssetItemAsync(
                    item: _item,
                    newKaryalyaId: selKaryalya.Id,
                    newKaryalyaName: selKaryalya.Name,
                    newDepartmentId: selDept.Id,
                    newDepartmentName: selDept.Name,
                    newRoomNo: roomNo,
                    moveDate: moveDate,
                    remarks: remarks);

                if (success)
                {
                    MessageBox.Show($"Asset '{_item.ItemUniqueCode}' relocated successfully to {selKaryalya.Name} > {selDept.Name}.", "Asset Relocated", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Could not complete the relocation. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SaveMoveButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not move asset.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SaveMoveButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
