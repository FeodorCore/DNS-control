using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Desktop.Models;
using Npgsql;

namespace Desktop.Data.Repositories;

public class CategoryRepository : BaseRepository
{
    public CategoryRepository(string connectionString) : base(connectionString) { }

    public async Task<List<Category>> GetAllAsync()
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

    public async Task AddAsync(Category category)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO category (name) VALUES (@p1) RETURNING category_id", conn);
        cmd.Parameters.AddWithValue("p1", category.Name);
        category.CategoryId = (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Category category)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE category SET name = @p1 WHERE category_id = @p2", conn);
        cmd.Parameters.AddWithValue("p1", category.Name);
        cmd.Parameters.AddWithValue("p2", category.CategoryId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM category WHERE category_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasProductsAsync(int categoryId)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM product WHERE category_id = @p1", conn);
        cmd.Parameters.AddWithValue("p1", categoryId);
        var count = await cmd.ExecuteScalarAsync();
        return count is not null and not DBNull && Convert.ToInt64(count) > 0;
    }
}