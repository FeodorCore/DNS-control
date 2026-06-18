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

    /// <summary>
    /// Автоматически создает таблицы и индексы, если их нет в базе данных.
    /// </summary>
    public static async Task InitializeSchemaAsync(string connectionString)
    {
        const string schemaSql = @"
            CREATE TABLE IF NOT EXISTS category (
                category_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name        TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS supplier (
                supplier_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name        TEXT NOT NULL,
                phone       TEXT,
                email       TEXT
            );

            CREATE TABLE IF NOT EXISTS product (
                product_id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name                TEXT           NOT NULL,
                description         TEXT,
                current_price       DECIMAL(10,2) NOT NULL CHECK (current_price >= 0),
                last_purchase_price DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (last_purchase_price >= 0),
                average_cost        DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (average_cost >= 0),
                stock_quantity      INT           NOT NULL CHECK (stock_quantity >= 0),
                category_id         INT           NOT NULL REFERENCES category(category_id)
            );

            CREATE TABLE IF NOT EXISTS supply (
                supply_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                supplier_id INT           NOT NULL REFERENCES supplier(supplier_id),
                supply_date DATE          NOT NULL,
                total_cost  DECIMAL(12,2) NOT NULL CHECK (total_cost >= 0)
            );

            CREATE TABLE IF NOT EXISTS supply_item (
                supply_item_id      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                supply_id           INT           NOT NULL REFERENCES supply(supply_id),
                product_id          INT           NOT NULL REFERENCES product(product_id),
                quantity            INT           NOT NULL CHECK (quantity > 0),
                unit_purchase_price DECIMAL(10,2) NOT NULL CHECK (unit_purchase_price > 0)
            );

            CREATE TABLE IF NOT EXISTS customer (
                customer_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name        TEXT NOT NULL,
                phone       TEXT,
                email       TEXT
            );

            CREATE TABLE IF NOT EXISTS sale (
                sale_id       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                customer_id   INT REFERENCES customer(customer_id),
                sale_datetime TIMESTAMP     NOT NULL,
                total_amount  DECIMAL(12,2) NOT NULL CHECK (total_amount >= 0)
            );

            CREATE TABLE IF NOT EXISTS sale_item (
                sale_item_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                sale_id         INT           NOT NULL REFERENCES sale(sale_id),
                product_id      INT           NOT NULL REFERENCES product(product_id),
                quantity        INT           NOT NULL CHECK (quantity > 0),
                unit_sale_price DECIMAL(10,2) NOT NULL CHECK (unit_sale_price > 0),
                unit_cost_price DECIMAL(10,2) NOT NULL CHECK (unit_cost_price >= 0)
            );

            CREATE INDEX IF NOT EXISTS idx_product_category ON product(category_id);
            CREATE INDEX IF NOT EXISTS idx_supply_supplier ON supply(supplier_id);
            CREATE INDEX IF NOT EXISTS idx_supply_item_supply ON supply_item(supply_id);
            CREATE INDEX IF NOT EXISTS idx_supply_item_product ON supply_item(product_id);
            CREATE INDEX IF NOT EXISTS idx_sale_customer ON sale(customer_id);
            CREATE INDEX IF NOT EXISTS idx_sale_item_sale ON sale_item(sale_id);
            CREATE INDEX IF NOT EXISTS idx_sale_item_product ON sale_item(product_id);
        ";

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(schemaSql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    // Категории
    public Task<List<Category>> GetCategoriesAsync() => _categories.GetAllAsync();
    public Task AddCategoryAsync(Category c) => _categories.AddAsync(c);
    public Task UpdateCategoryAsync(Category c) => _categories.UpdateAsync(c);
    public Task DeleteCategoryAsync(int id) => _categories.DeleteAsync(id);
    public Task<bool> HasProductsInCategoryAsync(int categoryId) => _categories.HasProductsAsync(categoryId);

    // Поставщики
    public Task<List<Supplier>> GetSuppliersAsync() => _suppliers.GetAllAsync();
    public Task AddSupplierAsync(Supplier s) => _suppliers.AddAsync(s);
    public Task UpdateSupplierAsync(Supplier s) => _suppliers.UpdateAsync(s);
    public Task DeleteSupplierAsync(int id) => _suppliers.DeleteAsync(id);
    public Task<bool> HasSuppliesForSupplierAsync(int supplierId) => _suppliers.HasSuppliesAsync(supplierId);

    // Покупатели
    public Task<List<Customer>> GetCustomersAsync() => _customers.GetAllAsync();
    public Task AddCustomerAsync(Customer c) => _customers.AddAsync(c);
    public Task UpdateCustomerAsync(Customer c) => _customers.UpdateAsync(c);
    public Task DeleteCustomerAsync(int id) => _customers.DeleteAsync(id);
    public Task<bool> HasSalesForCustomerAsync(int customerId) => _customers.HasSalesAsync(customerId);

    // Товары
    public Task<List<Product>> GetProductsAsync() => _products.GetAllAsync();
    public Task AddProductAsync(Product p) => _products.AddAsync(p);
    public Task UpdateProductAsync(Product p) => _products.UpdateAsync(p);
    public Task DeleteProductAsync(int id) => _products.DeleteAsync(id);
    public Task<int> GetProductStockAsync(int productId) => _products.GetStockAsync(productId);
    public Task<decimal> GetLastPurchasePriceAsync(int productId) => _products.GetLastPurchasePriceAsync(productId);
    
    public async Task<bool> HasRelationsForProductAsync(int productId)
    {
        var hasSupply = await _products.HasSupplyItemsAsync(productId);
        var hasSale = await _products.HasSaleItemsAsync(productId);
        return hasSupply || hasSale;
    }

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