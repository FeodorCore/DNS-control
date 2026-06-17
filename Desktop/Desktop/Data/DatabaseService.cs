using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Desktop.Data.Repositories;
using Desktop.Models;
using Desktop.Models.Reports;
using Npgsql;

namespace Desktop.Data;

public class DatabaseService
{
    private static DatabaseService? _instance;
    private readonly string _connectionString; // <-- сохраняем строку подключения

    public static DatabaseService Instance =>
        _instance ?? throw new InvalidOperationException(
            "DatabaseService не инициализирован. Вызовите Initialize() после успешного подключения.");

    private readonly CategoryRepository _categories;
    private readonly SupplierRepository _suppliers;
    private readonly ProductRepository _products;
    private readonly SupplyRepository _supplies;
    private readonly SaleRepository _sales;
    private readonly ReportRepository _reports;

    private DatabaseService(string connectionString)
    {
        _connectionString = connectionString; // <-- сохраняем
        _categories = new CategoryRepository(connectionString);
        _suppliers = new SupplierRepository(connectionString);
        _products = new ProductRepository(connectionString);
        _supplies = new SupplyRepository(connectionString);
        _sales = new SaleRepository(connectionString);
        _reports = new ReportRepository(connectionString);
    }

    public static void Initialize(string connectionString)
    {
        _instance = new DatabaseService(connectionString);
        _ = MigrateAsync();
    }

    private static async Task MigrateAsync()
    {
        try
        {
            // Используем сохранённую строку подключения из экземпляра
            var cs = _instance!._connectionString;
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();
            // Добавляем столбец average_cost, если его ещё нет
            await using var cmd = new NpgsqlCommand(
                "ALTER TABLE product ADD COLUMN IF NOT EXISTS average_cost DECIMAL(10,2) NOT NULL DEFAULT 0",
                conn);
            await cmd.ExecuteNonQueryAsync();

            // Инициализируем average_cost у существующих товаров на основе последней цены закупки, если average_cost = 0
            await using var updateCmd = new NpgsqlCommand(
                "UPDATE product SET average_cost = last_purchase_price WHERE average_cost = 0 AND last_purchase_price > 0",
                conn);
            await updateCmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Игнорируем ошибки миграции (например, если БД недоступна)
        }
    }

    public static async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Категории
    public Task<List<Category>> GetCategoriesAsync() => _categories.GetAllAsync();
    public Task AddCategoryAsync(Category c) => _categories.AddAsync(c);
    public Task UpdateCategoryAsync(Category c) => _categories.UpdateAsync(c);
    public Task DeleteCategoryAsync(int id) => _categories.DeleteAsync(id);

    // Поставщики
    public Task<List<Supplier>> GetSuppliersAsync() => _suppliers.GetAllAsync();
    public Task AddSupplierAsync(Supplier s) => _suppliers.AddAsync(s);
    public Task UpdateSupplierAsync(Supplier s) => _suppliers.UpdateAsync(s);
    public Task DeleteSupplierAsync(int id) => _suppliers.DeleteAsync(id);

    // Товары
    public Task<List<Product>> GetProductsAsync() => _products.GetAllAsync();
    public Task AddProductAsync(Product p) => _products.AddAsync(p);
    public Task UpdateProductAsync(Product p) => _products.UpdateAsync(p);
    public Task DeleteProductAsync(int id) => _products.DeleteAsync(id);
    public Task<int> GetProductStockAsync(int productId) => _products.GetStockAsync(productId);
    public Task<decimal> GetLastPurchasePriceAsync(int productId) => _products.GetLastPurchasePriceAsync(productId);

    // Поставки
    public Task SaveSupplyAsync(Supply supply, List<SupplyItem> items) => _supplies.SaveAsync(supply, items);

    // Продажи
    public Task SaveSaleAsync(Sale sale, List<SaleItem> items) => _sales.SaveAsync(sale, items);

    // Отчёты
    public Task<List<StockReportRow>> GetStockReportAsync(string categoryName) => _reports.GetStockReportAsync(categoryName);

    public Task<List<SalesByDayReportRow>> GetSalesByDayReportAsync(
        DateTime dateFrom, DateTime dateTo, string categoryName)
        => _reports.GetSalesByDayReportAsync(dateFrom, dateTo, categoryName);

    public Task<List<ProfitByProductReportRow>> GetProfitByProductReportAsync(
        DateTime dateFrom, DateTime dateTo, string categoryName)
        => _reports.GetProfitByProductReportAsync(dateFrom, dateTo, categoryName);
}