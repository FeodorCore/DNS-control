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

    public static DatabaseService Instance =>
        _instance ?? throw new InvalidOperationException(
            "DatabaseService не инициализирован. Вызовите Initialize() после успешного подключения.");

    // Репозитории
    private readonly CategoryRepository _categories;
    private readonly SupplierRepository _suppliers;
    private readonly ProductRepository _products;
    private readonly SupplyRepository _supplies;
    private readonly SaleRepository _sales;
    private readonly ReportRepository _reports;

    private DatabaseService(string connectionString)
    {
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