using System;
using System.Collections.Generic;

namespace Desktop.Models;

public class Sale
{
    public int SaleId { get; set; }
    public int? CustomerId { get; set; } // Nullable, так как продажа может быть без регистрации покупателя
    public string? CustomerName { get; set; } // Для отображения
    public DateTime SaleDatetime { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SaleItem> Items { get; set; } = new();
}