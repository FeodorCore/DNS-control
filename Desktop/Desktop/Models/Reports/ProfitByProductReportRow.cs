namespace Desktop.Models.Reports;

public class ProfitByProductReportRow
{
    public string ProductName { get; set; } = string.Empty;
    public int SoldQuantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
}