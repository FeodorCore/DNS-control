using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Desktop.Models.Reports;

namespace Desktop.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    [ObservableProperty] private string _selectedReport = "Товары на складе";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _selectedCategory = "Все";

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

    // Три типизированные коллекции — каждая для своего DataGrid
    [ObservableProperty] private ObservableCollection<StockReportRow> _stockRows = new();
    [ObservableProperty] private ObservableCollection<SalesByDayReportRow> _salesByDayRows = new();
    [ObservableProperty] private ObservableCollection<ProfitByProductReportRow> _profitRows = new();

    public ObservableCollection<string> ReportTypes { get; } = new()
    {
        "Товары на складе", "Продажи по дням", "Прибыль по товарам"
    };

    public ObservableCollection<string> Categories { get; } = new();

    // Вычисляемые свойства для IsVisible в XAML
    public bool IsStockReport => SelectedReport == "Товары на складе";
    public bool IsSalesByDayReport => SelectedReport == "Продажи по дням";
    public bool IsProfitReport => SelectedReport == "Прибыль по товарам";

    // Итоги (для красивого отображения под таблицей)
    [ObservableProperty] private decimal _totalSales;
    [ObservableProperty] private decimal _totalProfit;
    [ObservableProperty] private int _totalChecks;

    public ReportsViewModel()
    {
        _ = LoadCategoriesAsync();
    }

    partial void OnSelectedReportChanged(string value)
    {
        OnPropertyChanged(nameof(IsStockReport));
        OnPropertyChanged(nameof(IsSalesByDayReport));
        OnPropertyChanged(nameof(IsProfitReport));
        ClearAll();
    }

    private void ClearAll()
    {
        StockRows.Clear();
        SalesByDayRows.Clear();
        ProfitRows.Clear();
        TotalSales = 0;
        TotalProfit = 0;
        TotalChecks = 0;
    }

    private async Task LoadCategoriesAsync()
    {
        var cats = await DatabaseService.Instance.GetCategoriesAsync();
        var names = cats.Select(c => c.Name).ToList();
        names.Insert(0, "Все");
        Categories.Clear();
        foreach (var n in names) Categories.Add(n);
        SelectedCategory = "Все";
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        ErrorMessage = null;
        ClearAll();

        try
        {
            var db = DatabaseService.Instance;
            var from = DateFrom?.DateTime ?? DateTime.MinValue;
            var to = DateTo?.DateTime ?? DateTime.MaxValue;

            switch (SelectedReport)
            {
                case "Товары на складе":
                    var stock = await db.GetStockReportAsync();
                    StockRows = new ObservableCollection<StockReportRow>(stock);
                    if (StockRows.Count == 0)
                        ErrorMessage = "Нет товаров на складе.";
                    break;

                case "Продажи по дням":
                    var sales = await db.GetSalesByDayReportAsync(from, to, SelectedCategory);
                    SalesByDayRows = new ObservableCollection<SalesByDayReportRow>(sales);
                    TotalChecks = SalesByDayRows.Sum(r => r.ChecksCount);
                    TotalSales = SalesByDayRows.Sum(r => r.SalesAmount);
                    TotalProfit = SalesByDayRows.Sum(r => r.Profit);
                    if (SalesByDayRows.Count == 0)
                        ErrorMessage = "Нет данных для выбранного периода / категории.";
                    break;

                case "Прибыль по товарам":
                    var profit = await db.GetProfitByProductReportAsync(from, to, SelectedCategory);
                    ProfitRows = new ObservableCollection<ProfitByProductReportRow>(profit);
                    TotalSales = ProfitRows.Sum(r => r.Revenue);
                    TotalProfit = ProfitRows.Sum(r => r.Profit);
                    if (ProfitRows.Count == 0)
                        ErrorMessage = "Нет данных для выбранного периода / категории.";
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка формирования отчёта: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        ErrorMessage = null;

        if (StockRows.Count == 0 && SalesByDayRows.Count == 0 && ProfitRows.Count == 0)
        {
            ErrorMessage = "Сначала сформируйте отчёт, чтобы экспортировать его.";
            return;
        }

        var window = App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (window is null) return;

        var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            DefaultExtension = "xlsx",
            SuggestedFileName = $"Отчёт_{SelectedReport}_{DateTime.Now:yyyy-MM-dd}.xlsx",
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

        switch (SelectedReport)
        {
            case "Товары на складе":
                var ws1 = wb.Worksheets.Add("Остатки");
                ws1.Cell(1, 1).Value = "Товар";
                ws1.Cell(1, 2).Value = "Категория";
                ws1.Cell(1, 3).Value = "Остаток";
                ws1.Cell(1, 4).Value = "Цена";
                ws1.Range(1, 1, 1, 4).Style.Font.Bold = true;
                int r1 = 2;
                foreach (var row in StockRows)
                {
                    ws1.Cell(r1, 1).Value = row.ProductName;
                    ws1.Cell(r1, 2).Value = row.CategoryName;
                    ws1.Cell(r1, 3).Value = row.Stock;
                    ws1.Cell(r1, 4).Value = row.Price;
                    r1++;
                }

                break;

            case "Продажи по дням":
                var ws2 = wb.Worksheets.Add("Продажи");
                ws2.Cell(1, 1).Value = "Дата";
                ws2.Cell(1, 2).Value = "Чеков";
                ws2.Cell(1, 3).Value = "Сумма продаж";
                ws2.Cell(1, 4).Value = "Прибыль";
                ws2.Range(1, 1, 1, 4).Style.Font.Bold = true;
                int r2 = 2;
                foreach (var row in SalesByDayRows)
                {
                    ws2.Cell(r2, 1).Value = row.Date.ToString("yyyy-MM-dd");
                    ws2.Cell(r2, 2).Value = row.ChecksCount;
                    ws2.Cell(r2, 3).Value = row.SalesAmount;
                    ws2.Cell(r2, 4).Value = row.Profit;
                    r2++;
                }

                break;

            case "Прибыль по товарам":
                var ws3 = wb.Worksheets.Add("Прибыль");
                ws3.Cell(1, 1).Value = "Товар";
                ws3.Cell(1, 2).Value = "Продано";
                ws3.Cell(1, 3).Value = "Выручка";
                ws3.Cell(1, 4).Value = "Прибыль";
                ws3.Range(1, 1, 1, 4).Style.Font.Bold = true;
                int r3 = 2;
                foreach (var row in ProfitRows)
                {
                    ws3.Cell(r3, 1).Value = row.ProductName;
                    ws3.Cell(r3, 2).Value = row.SoldQuantity;
                    ws3.Cell(r3, 3).Value = row.Revenue;
                    ws3.Cell(r3, 4).Value = row.Profit;
                    r3++;
                }

                break;
        }

        wb.SaveAs(stream);
    }
}