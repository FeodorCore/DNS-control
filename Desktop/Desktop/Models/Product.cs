namespace Desktop.Models;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CurrentPrice { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; } // for display
}