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
    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
    [ObservableProperty] private ObservableCollection<Product> _products = new();

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

    [ObservableProperty] private ObservableCollection<SupplyItemViewModel> _items = new();
    public decimal OverallTotal => Items.Sum(i => i.Total);
    [ObservableProperty] private string? _errorMessage;

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
        newItem.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(SupplyItemViewModel.ProductId) && newItem.ProductId > 0)
            {
                var product = Products.FirstOrDefault(p => p.ProductId == newItem.ProductId);
                if (product != null)
                {
                    newItem.ProductName = product.Name;
                    newItem.CurrentStock = product.StockQuantity;
                    
                    // Узнаем, по какой цене мы покупали этот товар в последний раз
                    var lastPrice = await DatabaseService.Instance.GetLastPurchasePriceAsync(newItem.ProductId);
                    newItem.LastPurchasePrice = lastPrice ?? 0m;
                    
                    // АВТОЗАПОЛНЕНИЕ: Подставляем прошлую цену закупки, чтобы не вводить её вручную
                    if (newItem.UnitPurchasePrice == 0 && newItem.LastPurchasePrice > 0)
                    {
                        newItem.UnitPurchasePrice = newItem.LastPurchasePrice;
                    }
                }
            }
            OnPropertyChanged(nameof(OverallTotal));
        };
        
        Items.Add(newItem);
        OnPropertyChanged(nameof(OverallTotal));
        ErrorMessage = null;
    }

    [RelayCommand]
    private void DeleteItem(SupplyItemViewModel? item)
    {
        if (item is null) return;
        item.PropertyChanged -= (_, _) => OnPropertyChanged(nameof(OverallTotal));
        Items.Remove(item);
        OnPropertyChanged(nameof(OverallTotal));
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveSupplyAsync()
    {
        if (SupplierId == 0)
        {
            ErrorMessage = "Выберите поставщика.";
            return;
        }
        if (Items.Count == 0)
        {
            ErrorMessage = "Добавьте хотя бы одну позицию в поставку.";
            return;
        }

        foreach (var item in Items)
        {
            if (item.ProductId == 0)
            {
                ErrorMessage = "Для каждой позиции выберите товар.";
                return;
            }
            if (item.Quantity <= 0)
            {
                ErrorMessage = "Количество товара должно быть больше нуля.";
                return;
            }
            if (item.UnitPurchasePrice <= 0)
            {
                ErrorMessage = "Цена закупки должна быть больше нуля.";
                return;
            }
        }

        if (CurrentSupply is null) return;

        var db = DatabaseService.Instance;
        var itemsToSave = Items.Select(i => new SupplyItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPurchasePrice = i.UnitPurchasePrice
        }).ToList();

        CurrentSupply.TotalCost = OverallTotal;
        await db.SaveSupplyAsync(CurrentSupply, itemsToSave);

        Items.Clear();
        CurrentSupply = new Supply { SupplyDate = DateTime.Today };
        OnPropertyChanged(nameof(SupplyDate));
        OnPropertyChanged(nameof(SupplierId));
        OnPropertyChanged(nameof(OverallTotal));
        ErrorMessage = null;
    }
}