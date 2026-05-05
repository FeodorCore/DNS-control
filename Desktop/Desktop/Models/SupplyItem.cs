namespace Desktop.Models;

public class SupplyItem
{
    public int SupplyItemId { get; set; }
    public int SupplyId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public string? ProductName { get; set; } // for display
}