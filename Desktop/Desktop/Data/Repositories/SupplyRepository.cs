using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Desktop.Models;
using Npgsql;

namespace Desktop.Data.Repositories;

public class SupplyRepository : BaseRepository
{
    public SupplyRepository(string connectionString) : base(connectionString) { }

    public async Task SaveAsync(Supply supply, List<SupplyItem> items)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            if (supply.SupplyId == 0)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO supply (supplier_id, supply_date, total_cost) VALUES (@p1, @p2, @p3) RETURNING supply_id",
                    conn, tx);
                cmd.Parameters.AddWithValue("p1", supply.SupplierId);
                cmd.Parameters.AddWithValue("p2", supply.SupplyDate);
                cmd.Parameters.AddWithValue("p3", supply.TotalCost);
                var result = await cmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    throw new Exception("Не удалось получить ID поставки после вставки.");
                supply.SupplyId = Convert.ToInt32(result);
            }
            else
            {
                await using var cmd = new NpgsqlCommand(
                    "UPDATE supply SET supplier_id = @p1, supply_date = @p2, total_cost = @p3 WHERE supply_id = @p4", conn, tx);
                cmd.Parameters.AddWithValue("p1", supply.SupplierId);
                cmd.Parameters.AddWithValue("p2", supply.SupplyDate);
                cmd.Parameters.AddWithValue("p3", supply.TotalCost);
                cmd.Parameters.AddWithValue("p4", supply.SupplyId);
                await cmd.ExecuteNonQueryAsync();

                await using var delCmd = new NpgsqlCommand(
                    "DELETE FROM supply_item WHERE supply_id = @p1", conn, tx);
                delCmd.Parameters.AddWithValue("p1", supply.SupplyId);
                await delCmd.ExecuteNonQueryAsync();
            }

            foreach (var item in items)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO supply_item (supply_id, product_id, quantity, unit_purchase_price) VALUES (@p1, @p2, @p3, @p4)",
                    conn, tx);
                cmd.Parameters.AddWithValue("p1", supply.SupplyId);
                cmd.Parameters.AddWithValue("p2", item.ProductId);
                cmd.Parameters.AddWithValue("p3", item.Quantity);
                cmd.Parameters.AddWithValue("p4", item.UnitPurchasePrice);
                await cmd.ExecuteNonQueryAsync();

                // Обновляем остаток
                await using var upd = new NpgsqlCommand(
                    "UPDATE product SET stock_quantity = stock_quantity + @q WHERE product_id = @pid", conn, tx);
                upd.Parameters.AddWithValue("q", item.Quantity);
                upd.Parameters.AddWithValue("pid", item.ProductId);
                await upd.ExecuteNonQueryAsync();

                // Обновляем last_purchase_price
                await using var updPrice = new NpgsqlCommand(
                    @"UPDATE product SET last_purchase_price = @newPrice
                      WHERE product_id = @pid", conn, tx);
                updPrice.Parameters.AddWithValue("newPrice", item.UnitPurchasePrice);
                updPrice.Parameters.AddWithValue("pid", item.ProductId);
                await updPrice.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}