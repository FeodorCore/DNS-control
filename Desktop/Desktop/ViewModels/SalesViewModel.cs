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
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    
    private Sale? _currentSale;
    public Sale? CurrentSale
    {
        get => _currentSale;
        set
        {
            SetProperty(ref _currentSale, value);
            OnPropertyChanged(nameof(SaleDatetime));
            OnPropertyChanged(nameof(CustomerId));
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
    
    public int? CustomerId
    {
        get => CurrentSale?.CustomerId;
        set
        {
            if (CurrentSale != null)
            {
                CurrentSale.CustomerId = value;
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
        
        // Добавляем "Разовый покупатель" (ID = 0) в начало списка для удобства
        var customers = await db.GetCustomersAsync();
        var dummyCustomer = new Customer { CustomerId = 0, Name = "Разовый покупатель" };
        Customers = new ObservableCollection<Customer>(new[] { dummyCustomer }.Concat(customers));
        
        CurrentSale = new Sale { SaleDatetime = DateTime.Now, CustomerId = 0 };
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
                    newItem.UnitCostPrice = product.AverageCost;
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

            // Если выбран "Разовый покупатель" (CustomerId == 0), то в БД пишем null
            int? dbCustomerId = (CustomerId == 0) ? null : CustomerId;

            var itemsToSave = Items.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitSalePrice = i.UnitSalePrice,
                UnitCostPrice = i.UnitCostPrice
            }).ToList();

            CurrentSale.CustomerId = dbCustomerId;
            CurrentSale.TotalAmount = OverallTotal;
            
            await db.SaveSaleAsync(CurrentSale, itemsToSave);
            await RefreshProductsAsync();
            
            Items.Clear();
            CurrentSale = new Sale { SaleDatetime = DateTime.Now, CustomerId = 0 };
            OnPropertyChanged(nameof(SaleDatetime));
            OnPropertyChanged(nameof(CustomerId));
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