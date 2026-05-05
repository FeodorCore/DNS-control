using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Desktop.Models;

namespace Desktop.ViewModels;

public partial class SuppliersViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();
    [ObservableProperty]
    private Supplier? _selectedSupplier;

    public SuppliersViewModel() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        var list = await DatabaseService.Instance.GetSuppliersAsync();
        Suppliers = new ObservableCollection<Supplier>(list);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var supplier = new Supplier { Name = "Новый поставщик" };
        await DatabaseService.Instance.AddSupplierAsync(supplier);
        Suppliers.Add(supplier);
        SelectedSupplier = supplier;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedSupplier is null) return;
        await DatabaseService.Instance.DeleteSupplierAsync(SelectedSupplier.SupplierId);
        Suppliers.Remove(SelectedSupplier);
        SelectedSupplier = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedSupplier is null) return;
        await DatabaseService.Instance.UpdateSupplierAsync(SelectedSupplier);
        var idx = Suppliers.IndexOf(SelectedSupplier);
        if (idx >= 0) Suppliers[idx] = SelectedSupplier;
    }
}