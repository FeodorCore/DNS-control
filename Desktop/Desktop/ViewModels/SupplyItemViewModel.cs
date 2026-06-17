using CommunityToolkit.Mvvm.ComponentModel;

namespace Desktop.ViewModels;

public partial class SupplyItemViewModel : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string? _productName;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private decimal _unitPurchasePrice;

    public decimal Total => Quantity * UnitPurchasePrice;
    public bool IsValid => Quantity > 0 && UnitPurchasePrice > 0 && ProductId > 0;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(IsValid));
    }

    partial void OnUnitPurchasePriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(IsValid));
    }

    partial void OnProductIdChanged(int value)
    {
        OnPropertyChanged(nameof(IsValid));
    }
}