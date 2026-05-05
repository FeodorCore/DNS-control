namespace Desktop.Models;

public class SaleItem
{
    public int SaleItemId { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitSalePrice { get; set; }
    public decimal UnitCostPrice { get; set; }
    public string? ProductName { get; set; } // for display
}