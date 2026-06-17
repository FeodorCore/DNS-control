using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Desktop.Models;
using Npgsql;

namespace Desktop.Data.Repositories;

public class CustomerRepository : BaseRepository
{
    public CustomerRepository(string connectionString) : base(connectionString) { }

    public async Task<List<Customer>> GetAllAsync()
    {
        var list = new List<Customer>();
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT customer_id, name, phone, email FROM customer ORDER BY name", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Customer
            {
                CustomerId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }
        return list;
    }

    public async Task AddAsync(Customer customer)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO customer (name, phone, email) VALUES (@p1, @p2, @p3) RETURNING customer_id", conn);
        cmd.Parameters.AddWithValue("p1", customer.Name);
        cmd.Parameters.AddWithValue("p2", (object?)customer.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p3", (object?)customer.Email ?? DBNull.Value);
        customer.CustomerId = (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Customer customer)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE customer SET name = @p1, phone = @p2, email = @p3 WHERE customer_id = @p4", conn);
        cmd.Parameters.AddWithValue("p1", customer.Name);
        cmd.Parameters.AddWithValue("p2", (object?)customer.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p3", (object?)customer.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p4", customer.CustomerId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM customer WHERE customer_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasSalesAsync(int customerId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM sale WHERE customer_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", customerId);
        var count = await cmd.ExecuteScalarAsync();
        return count is not null and not DBNull && Convert.ToInt64(count) > 0;
    }
}