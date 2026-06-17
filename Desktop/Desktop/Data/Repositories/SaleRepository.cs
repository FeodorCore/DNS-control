using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Desktop.Models;
using Npgsql;

namespace Desktop.Data.Repositories;

public class SaleRepository : BaseRepository
{
    public SaleRepository(string connectionString) : base(connectionString) { }

    public async Task SaveAsync(Sale sale, List<SaleItem> items)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            if (sale.SaleId == 0)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO sale (sale_datetime, total_amount) VALUES (@p1, @p2) RETURNING sale_id", conn, tx);
                cmd.Parameters.AddWithValue("p1", sale.SaleDatetime);
                cmd.Parameters.AddWithValue("p2", sale.TotalAmount);

                var result = await cmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    throw new Exception("Не удалось получить ID продажи после вставки.");

                sale.SaleId = Convert.ToInt32(result);
            }
            else
            {
                await using var cmd = new NpgsqlCommand(
                    "UPDATE sale SET sale_datetime = @p1, total_amount = @p2 WHERE sale_id = @p3", conn, tx);
                cmd.Parameters.AddWithValue("p1", sale.SaleDatetime);
                cmd.Parameters.AddWithValue("p2", sale.TotalAmount);
                cmd.Parameters.AddWithValue("p3", sale.SaleId);
                await cmd.ExecuteNonQueryAsync();

                await using var delCmd = new NpgsqlCommand(
                    "DELETE FROM sale_item WHERE sale_id = @p1", conn, tx);
                delCmd.Parameters.AddWithValue("p1", sale.SaleId);
                await delCmd.ExecuteNonQueryAsync();
            }

            if (sale.SaleId <= 0)
                throw new InvalidOperationException("ID продажи не был установлен перед вставкой позиций.");

            foreach (var item in items)
            {
                // Получаем актуальную среднюю себестоимость на данный момент
                await using var costCmd = new NpgsqlCommand(
                    "SELECT average_cost FROM product WHERE product_id = @pid", conn, tx);
                costCmd.Parameters.AddWithValue("pid", item.ProductId);
                var costResult = await costCmd.ExecuteScalarAsync();
                decimal actualCost = costResult is not null and not DBNull ? (decimal)costResult : 0m;
                
                // Переопределяем себестоимость для точности
                item.UnitCostPrice = actualCost;

                // Вставляем позицию продажи
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO sale_item (sale_id, product_id, quantity, unit_sale_price, unit_cost_price) VALUES (@p1, @p2, @p3, @p4, @p5)",
                    conn, tx);
                cmd.Parameters.AddWithValue("p1", sale.SaleId);
                cmd.Parameters.AddWithValue("p2", item.ProductId);
                cmd.Parameters.AddWithValue("p3", item.Quantity);
                cmd.Parameters.AddWithValue("p4", item.UnitSalePrice);
                cmd.Parameters.AddWithValue("p5", item.UnitCostPrice);
                await cmd.ExecuteNonQueryAsync();

                // Списываем остаток (средняя себестоимость не меняется при продаже)
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
}