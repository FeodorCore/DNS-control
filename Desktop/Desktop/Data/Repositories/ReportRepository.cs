using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Desktop.Models.Reports;
using Npgsql;

namespace Desktop.Data.Repositories;

public class ReportRepository : BaseRepository
{
    public ReportRepository(string connectionString) : base(connectionString) { }

    public async Task<List<StockReportRow>> GetStockReportAsync(string categoryName)
    {
        var list = new List<StockReportRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
SELECT p.name, c.name, p.stock_quantity, p.current_price
FROM product p 
JOIN category c ON p.category_id = c.category_id
WHERE (@cat = 'Все' OR c.name = @cat)
ORDER BY c.name, p.name";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("cat", string.IsNullOrEmpty(categoryName) ? "Все" : categoryName);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new StockReportRow
            {
                ProductName = reader.GetString(0),
                CategoryName = reader.GetString(1),
                Stock = reader.GetInt32(2),
                Price = reader.GetDecimal(3)
            });
        }
        return list;
    }

    public async Task<List<SalesByDayReportRow>> GetSalesByDayReportAsync(
        DateTime dateFrom, DateTime dateTo, string categoryName)
    {
        var list = new List<SalesByDayReportRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
SELECT s.sale_datetime::date                    AS date,
COUNT(DISTINCT s.sale_id)                AS checks_count,
SUM(si.quantity * si.unit_sale_price)    AS sales_amount,
SUM(si.quantity * (si.unit_sale_price - si.unit_cost_price)) AS profit
FROM sale s
JOIN sale_item si ON s.sale_id = si.sale_id
JOIN product p    ON si.product_id = p.product_id
JOIN category c   ON p.category_id = c.category_id
WHERE s.sale_datetime::date BETWEEN @d1::date AND @d2::date
AND (@cat = 'Все' OR c.name = @cat)
GROUP BY s.sale_datetime::date
ORDER BY date";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("d1", dateFrom);
        cmd.Parameters.AddWithValue("d2", dateTo);
        cmd.Parameters.AddWithValue("cat", string.IsNullOrEmpty(categoryName) ? "Все" : categoryName);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SalesByDayReportRow
            {
                Date = reader.GetDateTime(0),
                ChecksCount = reader.GetInt32(1),
                SalesAmount = reader.GetDecimal(2),
                Profit = reader.GetDecimal(3)
            });
        }
        return list;
    }

    public async Task<List<ProfitByProductReportRow>> GetProfitByProductReportAsync(
        DateTime dateFrom, DateTime dateTo, string categoryName)
    {
        var list = new List<ProfitByProductReportRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
SELECT p.name                                                     AS name,
SUM(si.quantity)                                           AS sold_qty,
SUM(si.quantity * si.unit_sale_price)                      AS revenue,
SUM(si.quantity * (si.unit_sale_price - si.unit_cost_price)) AS profit
FROM sale_item si
JOIN product p ON si.product_id = p.product_id
JOIN sale    s ON si.sale_id    = s.sale_id
JOIN category c ON p.category_id = c.category_id
WHERE s.sale_datetime::date BETWEEN @d1::date AND @d2::date
AND (@cat = 'Все' OR c.name = @cat)
GROUP BY p.product_id, p.name
ORDER BY profit DESC";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("d1", dateFrom);
        cmd.Parameters.AddWithValue("d2", dateTo);
        cmd.Parameters.AddWithValue("cat", string.IsNullOrEmpty(categoryName) ? "Все" : categoryName);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ProfitByProductReportRow
            {
                ProductName = reader.GetString(0),
                SoldQuantity = reader.GetInt32(1),
                Revenue = reader.GetDecimal(2),
                Profit = reader.GetDecimal(3)
            });
        }
        return list;
    }
}