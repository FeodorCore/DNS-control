using CommunityToolkit.Mvvm.ComponentModel;

namespace Desktop.ViewModels;

public partial class SaleItemViewModel : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string? _productName;
    [ObservableProperty] private int _currentStock;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private decimal _unitSalePrice;
    [ObservableProperty] private decimal _unitCostPrice;
    
    // Цвет текста для предупреждения о нехватке на складе
    [ObservableProperty] private string _quantityColor = "#101828";

    public decimal Total => Quantity * UnitSalePrice;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(Total));
        UpdateStockWarning();
    }
    
    partial void OnCurrentStockChanged(int value)
    {
        UpdateStockWarning();
    }

    private void UpdateStockWarning()
    {
        // Если количество больше остатка - красим в красный (Danger color)
        QuantityColor = (CurrentStock > 0 && Quantity > CurrentStock) ? "#D83B01" : "#101828";
    }
}