using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Npgsql;

namespace Desktop.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _selectedReport = "Продажи по дням";

    private DateTimeOffset? _dateFrom = DateTime.Today.AddMonths(-1);
    public DateTimeOffset? DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    private DateTimeOffset? _dateTo = DateTime.Today;
    public DateTimeOffset? DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

    [ObservableProperty]
    private string _selectedCategory = "Все";

    public ObservableCollection<string> ReportTypes { get; } = new()
    {
        "Товары на складе", "Продажи по дням", "Прибыль по товарам"
    };

    public ObservableCollection<string> Categories { get; } = new();

    public ReportsViewModel() => _ = LoadCategoriesAsync();

    private async Task LoadCategoriesAsync()
    {
        var db = DatabaseService.Instance;
        var cats = await db.GetCategoriesAsync();
        var names = cats.Select(c => c.Name).ToList();
        names.Insert(0, "Все");
        Categories.Clear();
        foreach (var name in names)
            Categories.Add(name);
        SelectedCategory = "Все";
    }

    private async Task<DataTable?> GenerateDataTableAsync()
    {
        DataTable? dt = SelectedReport switch
        {
            "Товары на складе" => await GetStockReport(),
            "Продажи по дням" => await GetSalesByDayReport(),
            "Прибыль по товарам" => await GetProfitByProductReport(),
            _ => null
        };
        return dt;
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        var table = await GenerateDataTableAsync();
        if (table == null || table.Rows.Count == 0)
            return;

        var window = App.Current?.ApplicationLifetime is 
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (window is null) return;

        var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            DefaultExtension = "xlsx",
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Excel")
                {
                    Patterns = new[] { "*.xlsx" }
                }
            }
        };

        var file = await window.StorageProvider.SaveFilePickerAsync(options);
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        using var wb = new XLWorkbook();
        wb.Worksheets.Add(table, "Отчёт");
        wb.SaveAs(stream);
    }

    private async Task<DataTable> GetStockReport()
    {
        var dt = new DataTable();
        using var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=postgres;Username=admin;Password=admin");
        await conn.OpenAsync();
        var cmd = new NpgsqlCommand(
            @"SELECT p.name AS Товар, c.name AS Категория, p.stock_quantity AS Остаток, p.current_price AS Цена
              FROM product p JOIN category c ON p.category_id = c.category_id
              ORDER BY c.name, p.name", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        dt.Load(reader);
        return dt;
    }

    private async Task<DataTable> GetSalesByDayReport()
    {
        var dt = new DataTable();
        using var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=postgres;Username=admin;Password=admin");
        await conn.OpenAsync();
        var cmd = new NpgsqlCommand(
            @"SELECT s.sale_datetime::date AS Дата, COUNT(s.sale_id) AS КоличествоЧеков, SUM(s.total_amount) AS СуммаПродаж,
                     SUM(si.quantity * (si.unit_sale_price - si.unit_cost_price)) AS Прибыль
              FROM sale s
              JOIN sale_item si ON s.sale_id = si.sale_id
              JOIN product p ON si.product_id = p.product_id
              WHERE s.sale_datetime::date BETWEEN @d1::date AND @d2::date
              AND (@cat = 'Все' OR p.category_id = (SELECT category_id FROM category WHERE name = @cat))
              GROUP BY s.sale_datetime::date
              ORDER BY Дата", conn);
        cmd.Parameters.AddWithValue("d1", DateFrom?.DateTime ?? DateTime.MinValue);
        cmd.Parameters.AddWithValue("d2", DateTo?.DateTime ?? DateTime.MaxValue);
        cmd.Parameters.AddWithValue("cat", SelectedCategory);
        using var reader = await cmd.ExecuteReaderAsync();
        dt.Load(reader);
        return dt;
    }

    private async Task<DataTable> GetProfitByProductReport()
    {
        var dt = new DataTable();
        using var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=postgres;Username=admin;Password=admin");
        await conn.OpenAsync();
        var cmd = new NpgsqlCommand(
            @"SELECT p.name AS Товар, SUM(si.quantity) AS Продано, 
                     SUM(si.quantity * si.unit_sale_price) AS Выручка,
                     SUM(si.quantity * (si.unit_sale_price - si.unit_cost_price)) AS Прибыль
              FROM sale_item si JOIN product p ON si.product_id = p.product_id
              JOIN sale s ON si.sale_id = s.sale_id
              WHERE s.sale_datetime::date BETWEEN @d1::date AND @d2::date
              AND (@cat = 'Все' OR p.category_id = (SELECT category_id FROM category WHERE name = @cat))
              GROUP BY p.product_id, p.name
              ORDER BY Прибыль DESC", conn);
        cmd.Parameters.AddWithValue("d1", DateFrom?.DateTime ?? DateTime.MinValue);
        cmd.Parameters.AddWithValue("d2", DateTo?.DateTime ?? DateTime.MaxValue);
        cmd.Parameters.AddWithValue("cat", SelectedCategory);
        using var reader = await cmd.ExecuteReaderAsync();
        dt.Load(reader);
        return dt;
    }
}