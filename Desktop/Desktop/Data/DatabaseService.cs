using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Desktop.Models;
using Npgsql;

namespace Desktop.Data;

public class DatabaseService
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=admin;Password=admin";

    public static DatabaseService Instance { get; } = new();

    private DatabaseService() { }

    private NpgsqlConnection CreateConnection() => new(ConnectionString);

    // ---------- Categories ----------
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

    // ---------- Suppliers ----------
    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        var list = new List<Supplier>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT supplier_id, name, phone, email FROM supplier ORDER BY name", conn);
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

    // ---------- Products ----------
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

    // ---------- Supplies ----------
    public async Task<List<Supply>> GetSuppliesAsync()
    {
        var list = new List<Supply>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT supply_id, supplier_id, supply_date, total_cost FROM supply ORDER BY supply_date DESC", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new Supply
            {
                SupplyId = reader.GetInt32(0),
                SupplierId = reader.GetInt32(1),
                SupplyDate = reader.GetDateTime(2),
                TotalCost = reader.GetDecimal(3)
            });
        return list;
    }

    public async Task<Supply?> GetSupplyByIdAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT supply_id, supplier_id, supply_date, total_cost FROM supply WHERE supply_id=@p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var supply = new Supply
        {
            SupplyId = reader.GetInt32(0),
            SupplierId = reader.GetInt32(1),
            SupplyDate = reader.GetDateTime(2),
            TotalCost = reader.GetDecimal(3)
        };
        reader.Close();

        supply.Items = await GetSupplyItemsAsync(id);
        return supply;
    }

    public async Task<List<SupplyItem>> GetSupplyItemsAsync(int supplyId)
    {
        var items = new List<SupplyItem>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
@"SELECT si.supply_item_id, si.product_id, si.quantity, si.unit_purchase_price, p.name
FROM supply_item si JOIN product p ON si.product_id = p.product_id
WHERE si.supply_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", supplyId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(new SupplyItem
            {
                SupplyItemId = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                Quantity = reader.GetInt32(2),
                UnitPurchasePrice = reader.GetDecimal(3),
                ProductName = reader.GetString(4)
            });
        return items;
    }

    public async Task SaveSupplyAsync(Supply supply, List<SupplyItem> items)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            // Insert supply header
            if (supply.SupplyId == 0)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO supply (supplier_id, supply_date, total_cost) VALUES (@p1,@p2,@p3) RETURNING supply_id", conn, tx);
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
                // Remove old items
                await using var delCmd = new NpgsqlCommand("DELETE FROM supply_item WHERE supply_id=@p1", conn, tx);
                delCmd.Parameters.AddWithValue("p1", supply.SupplyId);
                await delCmd.ExecuteNonQueryAsync();
            }

            // Insert items
            foreach (var item in items)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO supply_item (supply_id, product_id, quantity, unit_purchase_price) VALUES (@p1,@p2,@p3,@p4)", conn, tx);
                cmd.Parameters.AddWithValue("p1", supply.SupplyId);
                cmd.Parameters.AddWithValue("p2", item.ProductId);
                cmd.Parameters.AddWithValue("p3", item.Quantity);
                cmd.Parameters.AddWithValue("p4", item.UnitPurchasePrice);
                await cmd.ExecuteNonQueryAsync();

                // Update stock
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

    // ---------- Sales ----------
    public async Task<List<Sale>> GetSalesAsync()
    {
        var list = new List<Sale>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT sale_id, sale_datetime, total_amount FROM sale ORDER BY sale_datetime DESC", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new Sale
            {
                SaleId = reader.GetInt32(0),
                SaleDatetime = reader.GetDateTime(1),
                TotalAmount = reader.GetDecimal(2)
            });
        return list;
    }

    public async Task<Sale?> GetSaleByIdAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT sale_id, sale_datetime, total_amount FROM sale WHERE sale_id=@p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var sale = new Sale
        {
            SaleId = reader.GetInt32(0),
            SaleDatetime = reader.GetDateTime(1),
            TotalAmount = reader.GetDecimal(2)
        };
        reader.Close();
        sale.Items = await GetSaleItemsAsync(id);
        return sale;
    }

    public async Task<List<SaleItem>> GetSaleItemsAsync(int saleId)
    {
        var items = new List<SaleItem>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
@"SELECT si.sale_item_id, si.product_id, si.quantity, si.unit_sale_price, si.unit_cost_price, p.name
FROM sale_item si JOIN product p ON si.product_id = p.product_id
WHERE si.sale_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", saleId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(new SaleItem
            {
                SaleItemId = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                Quantity = reader.GetInt32(2),
                UnitSalePrice = reader.GetDecimal(3),
                UnitCostPrice = reader.GetDecimal(4),
                ProductName = reader.GetString(5)
            });
        return items;
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
                    "INSERT INTO sale_item (sale_id, product_id, quantity, unit_sale_price, unit_cost_price) VALUES (@p1,@p2,@p3,@p4,@p5)", conn, tx);
                cmd.Parameters.AddWithValue("p1", sale.SaleId);
                cmd.Parameters.AddWithValue("p2", item.ProductId);
                cmd.Parameters.AddWithValue("p3", item.Quantity);
                cmd.Parameters.AddWithValue("p4", item.UnitSalePrice);
                cmd.Parameters.AddWithValue("p5", item.UnitCostPrice);
                await cmd.ExecuteNonQueryAsync();

                // Reduce stock
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
}