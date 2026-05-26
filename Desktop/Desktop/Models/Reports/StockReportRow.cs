namespace Desktop.Models.Reports;

public class StockReportRow
{
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Price { get; set; }
}