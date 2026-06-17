using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Desktop.Models;
using Npgsql;

namespace Desktop.Data.Repositories;

public class ProductRepository : BaseRepository
{
    public ProductRepository(string connectionString) : base(connectionString) { }

    public async Task<List<Product>> GetAllAsync()
    {
        var list = new List<Product>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"SELECT p.product_id, p.name, p.description, p.current_price,
                      p.last_purchase_price, p.average_cost, p.stock_quantity, p.category_id,
                      c.name AS category_name
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
                LastPurchasePrice = reader.GetDecimal(4),
                AverageCost = reader.GetDecimal(5),
                StockQuantity = reader.GetInt32(6),
                CategoryId = reader.GetInt32(7),
                CategoryName = reader.GetString(8)
            });
        return list;
    }

    public async Task AddAsync(Product product)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO product (name, description, current_price, last_purchase_price,
                                   average_cost, stock_quantity, category_id)
               VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7) RETURNING product_id", conn);
        cmd.Parameters.AddWithValue("p1", product.Name);
        cmd.Parameters.AddWithValue("p2", (object?)product.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p3", product.CurrentPrice);
        cmd.Parameters.AddWithValue("p4", product.LastPurchasePrice);
        cmd.Parameters.AddWithValue("p5", product.AverageCost);
        cmd.Parameters.AddWithValue("p6", product.StockQuantity);
        cmd.Parameters.AddWithValue("p7", product.CategoryId);
        product.ProductId = (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Product product)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"UPDATE product SET name=@p1, description=@p2, current_price=@p3,
               last_purchase_price=@p4, average_cost=@p5, stock_quantity=@p6, category_id=@p7
               WHERE product_id=@p8", conn);
        cmd.Parameters.AddWithValue("p1", product.Name);
        cmd.Parameters.AddWithValue("p2", (object?)product.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p3", product.CurrentPrice);
        cmd.Parameters.AddWithValue("p4", product.LastPurchasePrice);
        cmd.Parameters.AddWithValue("p5", product.AverageCost);
        cmd.Parameters.AddWithValue("p6", product.StockQuantity);
        cmd.Parameters.AddWithValue("p7", product.CategoryId);
        cmd.Parameters.AddWithValue("p8", product.ProductId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM product WHERE product_id=@p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetStockAsync(int productId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT stock_quantity FROM product WHERE product_id = @pid", conn);
        cmd.Parameters.AddWithValue("pid", productId);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null and not DBNull ? Convert.ToInt32(result) : 0;
    }

    public async Task<decimal> GetLastPurchasePriceAsync(int productId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT last_purchase_price FROM product WHERE product_id = @pid", conn);
        cmd.Parameters.AddWithValue("pid", productId);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null and not DBNull ? (decimal)result : 0m;
    }

    public async Task<decimal> GetAverageCostAsync(int productId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT average_cost FROM product WHERE product_id = @pid", conn);
        cmd.Parameters.AddWithValue("pid", productId);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null and not DBNull ? (decimal)result : 0m;
    }

    public async Task<bool> HasSupplyItemsAsync(int productId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM supply_item WHERE product_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", productId);
        var count = await cmd.ExecuteScalarAsync();
        return count is not null and not DBNull && Convert.ToInt64(count) > 0;
    }

    public async Task<bool> HasSaleItemsAsync(int productId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM sale_item WHERE product_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", productId);
        var count = await cmd.ExecuteScalarAsync();
        return count is not null and not DBNull && Convert.ToInt64(count) > 0;
    }
}