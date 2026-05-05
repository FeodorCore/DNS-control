using System;
using System.Collections.Generic;

namespace Desktop.Models;

public class Supply
{
    public int SupplyId { get; set; }
    public int SupplierId { get; set; }
    public DateTime SupplyDate { get; set; }
    public decimal TotalCost { get; set; }
    public List<SupplyItem> Items { get; set; } = new();
}