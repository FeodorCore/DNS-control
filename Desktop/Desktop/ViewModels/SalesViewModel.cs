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
    [ObservableProperty] private ObservableCollection<Product> _products = new();

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

    [ObservableProperty] private ObservableCollection<SaleItemViewModel> _items = new();
    public decimal OverallTotal => Items.Sum(i => i.Total);
    [ObservableProperty] private string? _errorMessage;

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
                    newItem.CurrentStock = product.StockQuantity;
                    
                    // АВТОЗАПОЛНЕНИЕ: Берем актуальную цену продажи из карточки товара
                    newItem.UnitSalePrice = product.CurrentPrice; 
                    
                    // Подтягиваем себестоимость для расчета прибыли
                    var cost = await DatabaseService.Instance.GetLastPurchasePriceAsync(newItem.ProductId);
                    newItem.UnitCostPrice = cost ?? 0m;
                }
            }
            OnPropertyChanged(nameof(OverallTotal));
        };
        
        newItem.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OverallTotal));
        Items.Add(newItem);
        OnPropertyChanged(nameof(OverallTotal));
        ErrorMessage = null;
    }

    [RelayCommand]
    private void DeleteItem(SaleItemViewModel? item)
    {
        if (item is null) return;
        item.PropertyChanged -= (_, _) => OnPropertyChanged(nameof(OverallTotal));
        Items.Remove(item);
        OnPropertyChanged(nameof(OverallTotal));
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveSaleAsync()
    {
        if (Items.Count == 0)
        {
            ErrorMessage = "Добавьте хотя бы одну позицию в продажу.";
            return;
        }

        var db = DatabaseService.Instance;
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
            if (item.UnitSalePrice <= 0)
            {
                ErrorMessage = "Цена продажи должна быть больше нуля.";
                return;
            }
            
            // Жесткая проверка остатка на уровне БД
            var stock = await db.GetProductStockAsync(item.ProductId);
            if (stock < item.Quantity)
            {
                var product = Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                ErrorMessage = $"Недостаточно товара \"{product?.Name}\" на складе. Доступно: {stock}";
                return;
            }
        }

        if (CurrentSale is null) return;

        var itemsToSave = Items.Select(i => new SaleItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitSalePrice = i.UnitSalePrice,
            UnitCostPrice = i.UnitCostPrice
        }).ToList();

        CurrentSale.TotalAmount = OverallTotal;
        await db.SaveSaleAsync(CurrentSale, itemsToSave);

        Items.Clear();
        CurrentSale = new Sale { SaleDatetime = DateTime.Now };
        OnPropertyChanged(nameof(SaleDatetime));
        OnPropertyChanged(nameof(OverallTotal));
        ErrorMessage = null;
    }
}