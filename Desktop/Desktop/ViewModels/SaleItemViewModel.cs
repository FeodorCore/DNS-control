using CommunityToolkit.Mvvm.ComponentModel;

namespace Desktop.ViewModels;

public partial class SaleItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _productId;

    [ObservableProperty]
    private string? _productName;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _unitSalePrice;

    [ObservableProperty]
    private decimal _unitCostPrice;

    public decimal Total => Quantity * UnitSalePrice;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(Total));
    partial void OnUnitSalePriceChanged(decimal value) => OnPropertyChanged(nameof(Total));
}