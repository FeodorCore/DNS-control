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
    private readonly string _connectionString;
    
    public static DatabaseService Instance =>
        _instance ?? throw new InvalidOperationException(
            "DatabaseService не инициализирован. Вызовите Initialize() после успешного подключения.");

    private readonly CategoryRepository _categories;
    private readonly SupplierRepository _suppliers;
    private readonly CustomerRepository _customers;
    private readonly ProductRepository _products;
    private readonly SupplyRepository _supplies;
    private readonly SaleRepository _sales;
    private readonly ReportRepository _reports;

    private DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
        _categories = new CategoryRepository(connectionString);
        _suppliers = new SupplierRepository(connectionString);
        _customers = new CustomerRepository(connectionString);
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
            var cs = _instance!._connectionString;
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            // Миграция для average_cost
            await using var cmd1 = new NpgsqlCommand(
                "ALTER TABLE product ADD COLUMN IF NOT EXISTS average_cost DECIMAL(10,2) NOT NULL DEFAULT 0", conn);
            await cmd1.ExecuteNonQueryAsync();

            await using var cmd2 = new NpgsqlCommand(
                "UPDATE product SET average_cost = last_purchase_price WHERE average_cost = 0 AND last_purchase_price > 0", conn);
            await cmd2.ExecuteNonQueryAsync();
            
            // Миграция для таблицы customer
            await using var cmd3 = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS customer (
                    customer_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    name        TEXT NOT NULL,
                    phone       TEXT,
                    email       TEXT,
                    discount_percent DECIMAL(5,2) NOT NULL DEFAULT 0 CHECK (discount_percent >= 0 AND discount_percent <= 100)
                );", conn);
            await cmd3.ExecuteNonQueryAsync();

            // Миграция для customer_id в sale
            await using var cmd4 = new NpgsqlCommand(
                "ALTER TABLE sale ADD COLUMN IF NOT EXISTS customer_id INT REFERENCES customer(customer_id)", conn);
            await cmd4.ExecuteNonQueryAsync();
            
            // Индекс для customer_id
            await using var cmd5 = new NpgsqlCommand(
                "CREATE INDEX IF NOT EXISTS idx_sale_customer ON sale(customer_id)", conn);
            await cmd5.ExecuteNonQueryAsync();
        }
        catch
        {
            // Игнорируем ошибки миграции
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
    
    // Покупатели
    public Task<List<Customer>> GetCustomersAsync() => _customers.GetAllAsync();
    public Task AddCustomerAsync(Customer c) => _customers.AddAsync(c);
    public Task UpdateCustomerAsync(Customer c) => _customers.UpdateAsync(c);
    public Task DeleteCustomerAsync(int id) => _customers.DeleteAsync(id);

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