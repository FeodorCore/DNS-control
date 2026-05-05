using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Desktop.Models;

namespace Desktop.ViewModels;

public partial class SuppliesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    private Supply? _currentSupply;
    public Supply? CurrentSupply
    {
        get => _currentSupply;
        set
        {
            SetProperty(ref _currentSupply, value);
            OnPropertyChanged(nameof(SupplyDate));
            OnPropertyChanged(nameof(SupplierId));
        }
    }

    // DateTimeOffset? для DatePicker
    public DateTimeOffset? SupplyDate
    {
        get => CurrentSupply?.SupplyDate;
        set
        {
            if (CurrentSupply != null && value.HasValue)
            {
                CurrentSupply.SupplyDate = value.Value.DateTime;
                OnPropertyChanged();
            }
        }
    }

    public int SupplierId
    {
        get => CurrentSupply?.SupplierId ?? 0;
        set
        {
            if (CurrentSupply != null)
            {
                CurrentSupply.SupplierId = value;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    private ObservableCollection<SupplyItemViewModel> _items = new();

    public decimal OverallTotal => Items.Sum(i => i.Total);

    public SuppliesViewModel() => _ = InitializeAsync();

    private async Task InitializeAsync()
    {
        var db = DatabaseService.Instance;
        Suppliers = new ObservableCollection<Supplier>(await db.GetSuppliersAsync());
        Products = new ObservableCollection<Product>(await db.GetProductsAsync());
        CurrentSupply = new Supply { SupplyDate = DateTime.Today };
    }

    [RelayCommand]
    private void AddItem()
    {
        var newItem = new SupplyItemViewModel();
        newItem.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OverallTotal));
        Items.Add(newItem);
        OnPropertyChanged(nameof(OverallTotal));
    }

    [RelayCommand]
    private void DeleteItem(SupplyItemViewModel? item)
    {
        if (item is null) return;
        item.PropertyChanged -= (_, _) => OnPropertyChanged(nameof(OverallTotal));
        Items.Remove(item);
        OnPropertyChanged(nameof(OverallTotal));
    }

    [RelayCommand]
    private async Task SaveSupplyAsync()
    {
        if (CurrentSupply is null) return;
        var db = DatabaseService.Instance;
        var items = Items.Select(i => new SupplyItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPurchasePrice = i.UnitPurchasePrice
        }).ToList();
        CurrentSupply.TotalCost = OverallTotal;
        await db.SaveSupplyAsync(CurrentSupply, items);
        Items.Clear();
        CurrentSupply = new Supply { SupplyDate = DateTime.Today };
        OnPropertyChanged(nameof(SupplyDate));
        OnPropertyChanged(nameof(SupplierId));
        OnPropertyChanged(nameof(OverallTotal));
    }
}