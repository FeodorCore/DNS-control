using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Desktop.Models;

namespace Desktop.ViewModels;

public partial class SalesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    private Sale? _currentSale;
    public Sale? CurrentSale
    {
        get => _currentSale;
        set
        {
            SetProperty(ref _currentSale, value);
            OnPropertyChanged(nameof(SaleDatetime));
        }
    }

    // DateTimeOffset? для DatePicker
    public DateTimeOffset? SaleDatetime
    {
        get => CurrentSale?.SaleDatetime;
        set
        {
            if (CurrentSale != null && value.HasValue)
            {
                CurrentSale.SaleDatetime = value.Value.DateTime;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    private ObservableCollection<SaleItemViewModel> _items = new();

    public decimal OverallTotal => Items.Sum(i => i.Total);

    public SalesViewModel() => _ = InitializeAsync();

    private async Task InitializeAsync()
    {
        var db = DatabaseService.Instance;
        Products = new ObservableCollection<Product>(await db.GetProductsAsync());
        CurrentSale = new Sale { SaleDatetime = DateTime.Now };
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var newItem = new SaleItemViewModel();
        newItem.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(SaleItemViewModel.ProductId) && newItem.ProductId > 0)
            {
                var product = Products.FirstOrDefault(p => p.ProductId == newItem.ProductId);
                if (product != null)
                {
                    newItem.ProductName = product.Name;
                    // Загружаем реальную себестоимость из последней поставки
                    var cost = await DatabaseService.Instance.GetLastPurchasePriceAsync(newItem.ProductId);
                    newItem.UnitCostPrice = cost ?? 0m;
                }
            }
        };
        newItem.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OverallTotal));
        Items.Add(newItem);
        OnPropertyChanged(nameof(OverallTotal));
    }

    [RelayCommand]
    private void DeleteItem(SaleItemViewModel? item)
    {
        if (item is null) return;
        item.PropertyChanged -= (_, _) => OnPropertyChanged(nameof(OverallTotal));
        Items.Remove(item);
        OnPropertyChanged(nameof(OverallTotal));
    }

    [RelayCommand]
    private async Task SaveSaleAsync()
    {
        if (CurrentSale is null) return;
        var db = DatabaseService.Instance;
        var items = Items.Select(i => new SaleItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitSalePrice = i.UnitSalePrice,
            UnitCostPrice = i.UnitCostPrice
        }).ToList();
        CurrentSale.TotalAmount = OverallTotal;
        await db.SaveSaleAsync(CurrentSale, items);
        Items.Clear();
        CurrentSale = new Sale { SaleDatetime = DateTime.Now };
        OnPropertyChanged(nameof(SaleDatetime));
        OnPropertyChanged(nameof(OverallTotal));
    }
}