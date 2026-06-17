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
                // Получаем текущие остаток и среднюю себестоимость (блокируем строку)
                await using var getCmd = new NpgsqlCommand(
                    "SELECT stock_quantity, average_cost FROM product WHERE product_id = @pid FOR UPDATE",
                    conn, tx);
                getCmd.Parameters.AddWithValue("pid", item.ProductId);
                using var reader = await getCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new Exception($"Товар с ID {item.ProductId} не найден.");

                int currentStock = reader.GetInt32(0);
                decimal currentAvg = reader.GetDecimal(1);
                reader.Close();

                // Вычисляем новую среднюю себестоимость
                int newStock = currentStock + item.Quantity;
                decimal newAvg;
                if (newStock == 0)
                {
                    newAvg = 0; // не должно случиться, но предохраняемся
                }
                else
                {
                    newAvg = Math.Round(
                        ((currentStock * currentAvg) + (item.Quantity * item.UnitPurchasePrice)) / newStock,
                        2,
                        MidpointRounding.AwayFromZero);
                }

                // Вставка позиции поставки
                await using var insertItemCmd = new NpgsqlCommand(
                    "INSERT INTO supply_item (supply_id, product_id, quantity, unit_purchase_price) VALUES (@p1, @p2, @p3, @p4)",
                    conn, tx);
                insertItemCmd.Parameters.AddWithValue("p1", supply.SupplyId);
                insertItemCmd.Parameters.AddWithValue("p2", item.ProductId);
                insertItemCmd.Parameters.AddWithValue("p3", item.Quantity);
                insertItemCmd.Parameters.AddWithValue("p4", item.UnitPurchasePrice);
                await insertItemCmd.ExecuteNonQueryAsync();

                // Обновляем остаток, среднюю себестоимость и последнюю цену закупки
                await using var upd = new NpgsqlCommand(
                    @"UPDATE product 
                      SET stock_quantity = @stock,
                          average_cost = @avg,
                          last_purchase_price = @lastPrice
                      WHERE product_id = @pid",
                    conn, tx);
                upd.Parameters.AddWithValue("stock", newStock);
                upd.Parameters.AddWithValue("avg", newAvg);
                upd.Parameters.AddWithValue("lastPrice", item.UnitPurchasePrice);
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
}