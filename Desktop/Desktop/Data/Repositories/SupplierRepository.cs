using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Desktop.Models;
using Npgsql;

namespace Desktop.Data.Repositories;

public class SupplierRepository : BaseRepository
{
    public SupplierRepository(string connectionString) : base(connectionString) { }

    public async Task<List<Supplier>> GetAllAsync()
    {
        var list = new List<Supplier>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT supplier_id, name, phone, email FROM supplier ORDER BY name", conn);
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

    public async Task AddAsync(Supplier supplier)
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

    public async Task UpdateAsync(Supplier supplier)
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

    public async Task DeleteAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM supplier WHERE supplier_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasSuppliesAsync(int supplierId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM supply WHERE supplier_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", supplierId);
        var count = await cmd.ExecuteScalarAsync();
        return count is not null and not DBNull && Convert.ToInt64(count) > 0;
    }
}