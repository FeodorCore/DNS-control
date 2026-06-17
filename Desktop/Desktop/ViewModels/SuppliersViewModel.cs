using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Desktop.Models;

namespace Desktop.ViewModels;

public partial class SuppliersViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
    [ObservableProperty] private Supplier? _selectedSupplier;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _canDelete = true;

    private List<Supplier> _allSuppliers = new();

    public SuppliersViewModel() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        _allSuppliers = await DatabaseService.Instance.GetSuppliersAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    async partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (value is null)
        {
            CanDelete = true;
            return;
        }
        CanDelete = !await DatabaseService.Instance.HasSuppliesForSupplierAsync(value.SupplierId);
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        IEnumerable<Supplier> filtered = _allSuppliers;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = _allSuppliers.Where(s =>
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        Suppliers = new ObservableCollection<Supplier>(filtered);
        if (SelectedSupplier != null && !Suppliers.Contains(SelectedSupplier))
            SelectedSupplier = Suppliers.FirstOrDefault();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var supplier = new Supplier { Name = "Новый поставщик" };
        await DatabaseService.Instance.AddSupplierAsync(supplier);
        _allSuppliers.Add(supplier);
        ApplyFilter();
        SelectedSupplier = supplier;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedSupplier is null) return;
        await DatabaseService.Instance.DeleteSupplierAsync(SelectedSupplier.SupplierId);
        _allSuppliers.RemoveAll(s => s.SupplierId == SelectedSupplier.SupplierId);
        SelectedSupplier = null;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedSupplier is null) return;
        await DatabaseService.Instance.UpdateSupplierAsync(SelectedSupplier);
        var idx = _allSuppliers.FindIndex(s => s.SupplierId == SelectedSupplier.SupplierId);
        if (idx >= 0) _allSuppliers[idx] = SelectedSupplier;
        ApplyFilter();
    }
}