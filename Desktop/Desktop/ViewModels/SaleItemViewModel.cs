using CommunityToolkit.Mvvm.ComponentModel;

namespace Desktop.ViewModels;

public partial class SaleItemViewModel : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string? _productName;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private decimal _unitSalePrice;
    [ObservableProperty] private decimal _unitCostPrice;
    [ObservableProperty] private int _maxStock;

    public decimal Total => Quantity * UnitSalePrice;
    public bool IsValid => Quantity > 0 && Quantity <= MaxStock && UnitSalePrice > 0 && ProductId > 0;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(IsValid));
    }

    partial void OnUnitSalePriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(IsValid));
    }

    partial void OnProductIdChanged(int value)
    {
        OnPropertyChanged(nameof(IsValid));
    }

    partial void OnMaxStockChanged(int value)
    {
        OnPropertyChanged(nameof(IsValid));
    }
}