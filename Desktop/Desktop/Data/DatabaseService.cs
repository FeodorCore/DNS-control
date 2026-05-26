using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Desktop.Models;
using Npgsql;
using Desktop.Models.Reports;

namespace Desktop.Data;

public class DatabaseService
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=admin;Password=admin";

    public static DatabaseService Instance { get; } = new();

    private DatabaseService()
    {
    }

    private NpgsqlConnection CreateConnection() => new(ConnectionString);

    // ========== Категории ==========
    public async Task<List<Category>> GetCategoriesAsync()
    {
        var list = new List<Category>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT category_id, name FROM category ORDER BY name", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new Category { CategoryId = reader.GetInt32(0), Name = reader.GetString(1) });
        return list;
    }

    public async Task AddCategoryAsync(Category category)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("INSERT INTO category (name) VALUES (@p1) RETURNING category_id", conn);
        cmd.Parameters.AddWithValue("p1", category.Name);
        category.CategoryId = (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("UPDATE category SET name = @p1 WHERE category_id = @p2", conn);
        cmd.Parameters.AddWithValue("p1", category.Name);
        cmd.Parameters.AddWithValue("p2", category.CategoryId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM category WHERE category_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ========== Поставщики ==========
    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        var list = new List<Supplier>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd =
            new NpgsqlCommand("SELECT supplier_id, name, phone, email FROM supplier ORDER BY name", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new Supplier
            {
                SupplierId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        return list;
    }

    public async Task AddSupplierAsync(Supplier supplier)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO supplier (name, phone, email) VALUES (@p1, @p2, @p3) RETURNING supplier_id", conn);
        cmd.Parameters.AddWithValue("p1", supplier.Name);
        cmd.Parameters.AddWithValue("p2", (object?)supplier.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p3", (object?)supplier.Email ?? DBNull.Value);
        supplier.SupplierId = (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateSupplierAsync(Supplier supplier)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE supplier SET name = @p1, phone = @p2, email = @p3 WHERE supplier_id = @p4", conn);
        cmd.Parameters.AddWithValue("p1", supplier.Name);
        cmd.Parameters.AddWithValue("p2", (object?)supplier.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p3", (object?)supplier.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p4", supplier.SupplierId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteSupplierAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM supplier WHERE supplier_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ========== Товары ==========
    public async Task<List<Product>> GetProductsAsync()
    {
        var list = new List<Product>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"SELECT p.product_id, p.name, p.description, p.current_price, p.stock_quantity, p.category_id, c.name AS category_name
              FROM product p JOIN category c ON p.category_id = c.category_id
              ORDER BY p.name", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new Product
            {
                ProductId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                CurrentPrice = reader.GetDecimal(3),
                StockQuantity = reader.GetInt32(4),
                CategoryId = reader.GetInt32(5),
                CategoryName = reader.GetString(6)
            });
        return list;
    }

    public async Task AddProductAsync(Product product)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO product (name, description, current_price, stock_quantity, category_id)
              VALUES (@p1, @p2, @p3, @p4, @p5) RETURNING product_id", conn);
        cmd.Parameters.AddWithValue("p1", product.Name);
        cmd.Parameters.AddWithValue("p2", (object?)product.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p3", product.CurrentPrice);
        cmd.Parameters.AddWithValue("p4", product.StockQuantity);
        cmd.Parameters.AddWithValue("p5", product.CategoryId);
        product.ProductId = (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateProductAsync(Product product)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"UPDATE product SET name=@p1, description=@p2, current_price=@p3, stock_quantity=@p4, category_id=@p5
              WHERE product_id=@p6", conn);
        cmd.Parameters.AddWithValue("p1", product.Name);
        cmd.Parameters.AddWithValue("p2", (object?)product.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p3", product.CurrentPrice);
        cmd.Parameters.AddWithValue("p4", product.StockQuantity);
        cmd.Parameters.AddWithValue("p5", product.CategoryId);
        cmd.Parameters.AddWithValue("p6", product.ProductId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteProductAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM product WHERE product_id=@p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ========== Поставки (только сохранение, без выборки) ==========
    public async Task SaveSupplyAsync(Supply supply, List<SupplyItem> items)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            if (supply.SupplyId == 0)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO supply (supplier_id, supply_date, total_cost) VALUES (@p1,@p2,@p3) RETURNING supply_id",
                    conn, tx);
                cmd.Parameters.AddWithValue("p1", supply.SupplierId);
                cmd.Parameters.AddWithValue("p2", supply.SupplyDate);
                cmd.Parameters.AddWithValue("p3", supply.TotalCost);
                supply.SupplyId = (int)(await cmd.ExecuteScalarAsync())!;
            }
            else
            {
                await using var cmd = new NpgsqlCommand(
                    "UPDATE supply SET supplier_id=@p1, supply_date=@p2, total_cost=@p3 WHERE supply_id=@p4", conn, tx);
                cmd.Parameters.AddWithValue("p1", supply.SupplierId);
                cmd.Parameters.AddWithValue("p2", supply.SupplyDate);
                cmd.Parameters.AddWithValue("p3", supply.TotalCost);
                cmd.Parameters.AddWithValue("p4", supply.SupplyId);
                await cmd.ExecuteNonQueryAsync();

                await using var delCmd = new NpgsqlCommand("DELETE FROM supply_item WHERE supply_id=@p1", conn, tx);
                delCmd.Parameters.AddWithValue("p1", supply.SupplyId);
                await delCmd.ExecuteNonQueryAsync();
            }

            foreach (var item in items)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO supply_item (supply_id, product_id, quantity, unit_purchase_price) VALUES (@p1,@p2,@p3,@p4)",
                    conn, tx);
                cmd.Parameters.AddWithValue("p1", supply.SupplyId);
                cmd.Parameters.AddWithValue("p2", item.ProductId);
                cmd.Parameters.AddWithValue("p3", item.Quantity);
                cmd.Parameters.AddWithValue("p4", item.UnitPurchasePrice);
                await cmd.ExecuteNonQueryAsync();

                await using var upd = new NpgsqlCommand(
                    "UPDATE product SET stock_quantity = stock_quantity + @q WHERE product_id = @pid", conn, tx);
                upd.Parameters.AddWithValue("q", item.Quantity);
                upd.Parameters.AddWithValue("pid", item.ProductId);
                await upd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task SaveSaleAsync(Sale sale, List<SaleItem> items)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            if (sale.SaleId == 0)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO sale (sale_datetime, total_amount) VALUES (@p1,@p2) RETURNING sale_id", conn, tx);
                cmd.Parameters.AddWithValue("p1", sale.SaleDatetime);
                cmd.Parameters.AddWithValue("p2", sale.TotalAmount);
                sale.SaleId = (int)(await cmd.ExecuteScalarAsync())!;
            }
            else
            {
                await using var cmd = new NpgsqlCommand(
                    "UPDATE sale SET sale_datetime=@p1, total_amount=@p2 WHERE sale_id=@p3", conn, tx);
                cmd.Parameters.AddWithValue("p1", sale.SaleDatetime);
                cmd.Parameters.AddWithValue("p2", sale.TotalAmount);
                cmd.Parameters.AddWithValue("p3", sale.SaleId);
                await cmd.ExecuteNonQueryAsync();

                await using var delCmd = new NpgsqlCommand("DELETE FROM sale_item WHERE sale_id=@p1", conn, tx);
                delCmd.Parameters.AddWithValue("p1", sale.SaleId);
                await delCmd.ExecuteNonQueryAsync();
            }

            foreach (var item in items)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO sale_item (sale_id, product_id, quantity, unit_sale_price, unit_cost_price) VALUES (@p1,@p2,@p3,@p4,@p5)",
                    conn, tx);
                cmd.Parameters.AddWithValue("p1", sale.SaleId);
                cmd.Parameters.AddWithValue("p2", item.ProductId);
                cmd.Parameters.AddWithValue("p3", item.Quantity);
                cmd.Parameters.AddWithValue("p4", item.UnitSalePrice);
                cmd.Parameters.AddWithValue("p5", item.UnitCostPrice);
                await cmd.ExecuteNonQueryAsync();

                await using var upd = new NpgsqlCommand(
                    "UPDATE product SET stock_quantity = stock_quantity - @q WHERE product_id = @pid", conn, tx);
                upd.Parameters.AddWithValue("q", item.Quantity);
                upd.Parameters.AddWithValue("pid", item.ProductId);
                await upd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ========== Вспомогательные методы ==========
    public async Task<decimal?> GetLastPurchasePriceAsync(int productId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            SELECT si.unit_purchase_price
            FROM supply_item si
            JOIN supply s ON si.supply_id = s.supply_id
            WHERE si.product_id = @pid
            ORDER BY s.supply_date DESC, s.supply_id DESC
            LIMIT 1", conn);
        cmd.Parameters.AddWithValue("pid", productId);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null and not DBNull ? (decimal)result : 0m;
    }

    public async Task<int> GetProductStockAsync(int productId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT stock_quantity FROM product WHERE product_id = @pid", conn);
        cmd.Parameters.AddWithValue("pid", productId);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null and not DBNull ? Convert.ToInt32(result) : 0;
    }


// ========== Отчёты ==========

    public async Task<List<StockReportRow>> GetStockReportAsync()
    {
        var list = new List<StockReportRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        const string sql = @"
        SELECT p.name, c.name, p.stock_quantity, p.current_price
        FROM product p JOIN category c ON p.category_id = c.category_id
        ORDER BY c.name, p.name";
        await using var cmd = new NpgsqlCommand(sql, conn);
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
        WHERE s.sale_datetime::date BETWEEN @d1::date AND @d2::date
          AND (@cat = 'Все' OR p.category_id = (SELECT category_id FROM category WHERE name = @cat))
        GROUP BY s.sale_datetime::date
        ORDER BY date";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("d1", dateFrom);
        cmd.Parameters.AddWithValue("d2", dateTo);
        cmd.Parameters.AddWithValue("cat", categoryName);
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
        WHERE s.sale_datetime::date BETWEEN @d1::date AND @d2::date
          AND (@cat = 'Все' OR p.category_id = (SELECT category_id FROM category WHERE name = @cat))
        GROUP BY p.product_id, p.name
        ORDER BY profit DESC";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("d1", dateFrom);
        cmd.Parameters.AddWithValue("d2", dateTo);
        cmd.Parameters.AddWithValue("cat", categoryName);
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