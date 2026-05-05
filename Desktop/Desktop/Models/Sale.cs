using System;
using System.Collections.Generic;

namespace Desktop.Models;

public class Sale
{
    public int SaleId { get; set; }
    public DateTime SaleDatetime { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SaleItem> Items { get; set; } = new();
}