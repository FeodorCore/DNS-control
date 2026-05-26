using System;

namespace Desktop.Models.Reports;

public class SalesByDayReportRow
{
    public DateTime Date { get; set; }
    public int ChecksCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal Profit { get; set; }
}