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
    [ObservableProperty] private bool _isProcessing;

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
        newItem.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SaleItemViewModel.ProductId) && newItem.ProductId > 0)
            {
                var product = Products.FirstOrDefault(p => p.ProductId == newItem.ProductId);
                if (product != null)
                {
                    newItem.ProductName = product.Name;
                    newItem.MaxStock = product.StockQuantity;
                    newItem.UnitCostPrice = product.LastPurchasePrice; // всегда актуальная себестоимость
                    if (newItem.UnitSalePrice <= 0)
                        newItem.UnitSalePrice = product.CurrentPrice;
                }
            }
            OnPropertyChanged(nameof(OverallTotal));
            ValidateItem(newItem);
        };
        Items.Add(newItem);
        OnPropertyChanged(nameof(OverallTotal));
        ErrorMessage = null;
    }

    private void ValidateItem(SaleItemViewModel item)
    {
        if (item.Quantity > item.MaxStock)
            ErrorMessage = $"Товар \"{item.ProductName}\": указано {item.Quantity}, доступно {item.MaxStock}";
        else
            ErrorMessage = null;
    }

    [RelayCommand]
    private void DeleteItem(SaleItemViewModel? item)
    {
        if (item is null) return;
        Items.Remove(item);
        OnPropertyChanged(nameof(OverallTotal));
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveSaleAsync()
    {
        if (IsProcessing) return;

        IsProcessing = true;
        ErrorMessage = null;

        try
        {
            if (Items.Count == 0)
            {
                ErrorMessage = "Добавьте хотя бы одну позицию в продажу.";
                return;
            }

            var db = DatabaseService.Instance;

            // Проверка обязательных полей в каждой позиции
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
            }

            // Группируем позиции по товару и проверяем суммарный остаток
            var grouped = Items.GroupBy(i => i.ProductId);
            foreach (var group in grouped)
            {
                var totalQty = group.Sum(i => i.Quantity);
                var stock = await db.GetProductStockAsync(group.Key);
                if (stock < totalQty)
                {
                    var product = Products.FirstOrDefault(p => p.ProductId == group.Key);
                    ErrorMessage = $"Недостаточно товара \"{product?.Name}\" на складе. " +
                                   $"Требуется {totalQty} шт., доступно {stock} шт.";
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
            await RefreshProductsAsync();

            // Сброс для следующего чека
            Items.Clear();
            CurrentSale = new Sale { SaleDatetime = DateTime.Now };
            OnPropertyChanged(nameof(SaleDatetime));
            OnPropertyChanged(nameof(OverallTotal));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task RefreshProductsAsync()
    {
        var db = DatabaseService.Instance;
        Products = new ObservableCollection<Product>(await db.GetProductsAsync());
    }
}