using CommunityToolkit.Mvvm.ComponentModel;

namespace Desktop.ViewModels;

public partial class SupplyItemViewModel : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string? _productName;
    [ObservableProperty] private int _currentStock;
    [ObservableProperty] private decimal _lastPurchasePrice;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private decimal _unitPurchasePrice;

    public decimal Total => Quantity * UnitPurchasePrice;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(Total));
    partial void OnUnitPurchasePriceChanged(decimal value) => OnPropertyChanged(nameof(Total));
}