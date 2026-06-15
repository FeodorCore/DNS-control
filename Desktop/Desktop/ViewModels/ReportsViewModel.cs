using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Desktop.Data;
using Desktop.Models.Reports;

namespace Desktop.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    [ObservableProperty] private string _selectedReport = "Товары на складе";
    [ObservableProperty] private string? _errorMessage;

    // Используем индекс вместо строки для надёжной работы ComboBox
    [ObservableProperty] private int _selectedCategoryIndex = 0;

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

    [ObservableProperty] private ObservableCollection<StockReportRow> _stockRows = new();
    [ObservableProperty] private ObservableCollection<SalesByDayReportRow> _salesByDayRows = new();
    [ObservableProperty] private ObservableCollection<ProfitByProductReportRow> _profitRows = new();

    public ObservableCollection<string> ReportTypes { get; } = new()
    {
        "Товары на складе", "Продажи по дням", "Прибыль по товарам"
    };

    // Коллекция категорий — "Все" всегда первый элемент (индекс 0)
    public ObservableCollection<string> Categories { get; } = new() { "Все" };

    public bool IsStockReport => SelectedReport == "Товары на складе";
    public bool IsSalesByDayReport => SelectedReport == "Продажи по дням";
    public bool IsProfitReport => SelectedReport == "Прибыль по товарам";

    [ObservableProperty] private decimal _totalSales;
    [ObservableProperty] private decimal _totalProfit;
    [ObservableProperty] private int _totalChecks;

    public ReportsViewModel()
    {
        _ = LoadCategoriesAsync();
    }

    // Если индекс сбросился в -1 (пустой выбор), возвращаем на 0 ("Все")
    partial void OnSelectedCategoryIndexChanged(int value)
    {
        if (value < 0 && Categories.Count > 0)
        {
            Dispatcher.UIThread.Post(() => SelectedCategoryIndex = 0);
        }
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
        try
        {
            var cats = await DatabaseService.Instance.GetCategoriesAsync();
            var names = cats.Select(c => c.Name).ToList();

            // Добавляем категории после уже существующего "Все"
            foreach (var n in names)
                Categories.Add(n);

            // Принудительно выбираем "Все" (индекс 0)
            SelectedCategoryIndex = 0;
        }
        catch
        {
            // Если БД недоступна, "Все" уже есть в коллекции
        }
    }

    /// <summary>
    /// Получает название выбранной категории по индексу
    /// </summary>
    private string GetSelectedCategoryName()
    {
        if (SelectedCategoryIndex >= 0 && SelectedCategoryIndex < Categories.Count)
            return Categories[SelectedCategoryIndex];
        return "Все";
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

            // Получаем категорию по индексу
            var catFilter = GetSelectedCategoryName();

            switch (SelectedReport)
            {
                case "Товары на складе":
                    var stock = await db.GetStockReportAsync(catFilter);
                    StockRows = new ObservableCollection<StockReportRow>(stock);
                    if (StockRows.Count == 0)
                        ErrorMessage = "Нет товаров на складе.";
                    break;
                case "Продажи по дням":
                    var sales = await db.GetSalesByDayReportAsync(from, to, catFilter);
                    SalesByDayRows = new ObservableCollection<SalesByDayReportRow>(sales);
                    TotalChecks = SalesByDayRows.Sum(r => r.ChecksCount);
                    TotalSales = SalesByDayRows.Sum(r => r.SalesAmount);
                    TotalProfit = SalesByDayRows.Sum(r => r.Profit);
                    if (SalesByDayRows.Count == 0)
                        ErrorMessage = "Нет данных для выбранного периода / категории.";
                    break;
                case "Прибыль по товарам":
                    var profit = await db.GetProfitByProductReportAsync(from, to, catFilter);
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

        // Вспомогательные методы для стилизации
        void SetupHeader(IXLWorksheet ws, string title, int colCount, out int headerStartRow)
        {
            // Заголовок отчета
            ws.Cell(1, 1).Value = title;
            var titleRange = ws.Range(1, 1, 1, colCount);
            titleRange.Merge();
            titleRange.Style.Font.FontSize = 18;
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontColor = XLColor.FromHtml("#ff9519"); // DNS Orange

            // Метаданные
            int metaRow = 3;
            ws.Cell(metaRow, 1).Value = $"Категория: {GetSelectedCategoryName()}";
            ws.Cell(metaRow, 1).Style.Font.Bold = true;
            ws.Cell(metaRow, 1).Style.Font.FontSize = 11;

            if (SelectedReport != "Товары на складе")
            {
                var from = DateFrom?.DateTime;
                var to = DateTo?.DateTime;
                string periodStr = "Период: ";
                if (from.HasValue && to.HasValue && from.Value.Year > 1 && to.Value.Year < 9999)
                {
                    periodStr += $"с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}";
                }
                else
                {
                    periodStr += "всё время";
                }

                ws.Cell(metaRow + 1, 1).Value = periodStr;
                ws.Cell(metaRow + 1, 1).Style.Font.Bold = true;
                ws.Cell(metaRow + 1, 1).Style.Font.FontSize = 11;

                ws.Cell(metaRow + 2, 1).Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                ws.Cell(metaRow + 2, 1).Style.Font.Italic = true;
                ws.Cell(metaRow + 2, 1).Style.Font.FontColor = XLColor.Gray;
                ws.Cell(metaRow + 2, 1).Style.Font.FontSize = 10;

                headerStartRow = metaRow + 4;
            }
            else
            {
                ws.Cell(metaRow + 1, 1).Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                ws.Cell(metaRow + 1, 1).Style.Font.Italic = true;
                ws.Cell(metaRow + 1, 1).Style.Font.FontColor = XLColor.Gray;
                ws.Cell(metaRow + 1, 1).Style.Font.FontSize = 10;

                headerStartRow = metaRow + 3;
            }
        }

        void StyleTableHeader(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#344054"); // Темный заголовок
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        }

        void StyleTableData(IXLRange range)
        {
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        switch (SelectedReport)
        {
            case "Товары на складе":
            {
                var ws = wb.Worksheets.Add("Остатки");
                SetupHeader(ws, "Отчёт: Товары на складе", 4, out int headerStartRow);

                ws.Cell(headerStartRow, 1).Value = "Товар";
                ws.Cell(headerStartRow, 2).Value = "Категория";
                ws.Cell(headerStartRow, 3).Value = "Остаток";
                ws.Cell(headerStartRow, 4).Value = "Цена, ₽";
                StyleTableHeader(ws.Range(headerStartRow, 1, headerStartRow, 4));

                int r = headerStartRow + 1;
                foreach (var row in StockRows)
                {
                    ws.Cell(r, 1).Value = row.ProductName;
                    ws.Cell(r, 2).Value = row.CategoryName;
                    ws.Cell(r, 3).Value = row.Stock;
                    ws.Cell(r, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 4).Value = row.Price;
                    ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                    r++;
                }

                if (StockRows.Count > 0)
                {
                    StyleTableData(ws.Range(headerStartRow + 1, 1, r - 1, 4));
                }

                break;
            }
            case "Продажи по дням":
            {
                var ws = wb.Worksheets.Add("Продажи");
                SetupHeader(ws, "Отчёт: Продажи по дням", 4, out int headerStartRow);

                ws.Cell(headerStartRow, 1).Value = "Дата";
                ws.Cell(headerStartRow, 2).Value = "Кол-во чеков";
                ws.Cell(headerStartRow, 3).Value = "Сумма продаж, ₽";
                ws.Cell(headerStartRow, 4).Value = "Прибыль, ₽";
                StyleTableHeader(ws.Range(headerStartRow, 1, headerStartRow, 4));

                int r = headerStartRow + 1;
                foreach (var row in SalesByDayRows)
                {
                    ws.Cell(r, 1).Value = row.Date.ToString("dd.MM.yyyy");
                    ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 2).Value = row.ChecksCount;
                    ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 3).Value = row.SalesAmount;
                    ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(r, 4).Value = row.Profit;
                    ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                    r++;
                }

                // Итоговая строка
                ws.Cell(r, 1).Value = "ИТОГО";
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Cell(r, 2).Value = TotalChecks;
                ws.Cell(r, 2).Style.Font.Bold = true;
                ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(r, 3).Value = TotalSales;
                ws.Cell(r, 3).Style.Font.Bold = true;
                ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(r, 4).Value = TotalProfit;
                ws.Cell(r, 4).Style.Font.Bold = true;
                ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(r, 4).Style.Font.FontColor = XLColor.FromHtml("#107C10"); // Зеленый

                if (SalesByDayRows.Count > 0)
                {
                    StyleTableData(ws.Range(headerStartRow + 1, 1, r - 1, 4));
                }

                ws.Range(r, 1, r, 4).Style.Border.TopBorder = XLBorderStyleValues.Medium;
                break;
            }
            case "Прибыль по товарам":
            {
                var ws = wb.Worksheets.Add("Прибыль");
                SetupHeader(ws, "Отчёт: Прибыль по товарам", 4, out int headerStartRow);

                ws.Cell(headerStartRow, 1).Value = "Товар";
                ws.Cell(headerStartRow, 2).Value = "Продано, шт.";
                ws.Cell(headerStartRow, 3).Value = "Выручка, ₽";
                ws.Cell(headerStartRow, 4).Value = "Прибыль, ₽";
                StyleTableHeader(ws.Range(headerStartRow, 1, headerStartRow, 4));

                int r = headerStartRow + 1;
                foreach (var row in ProfitRows)
                {
                    ws.Cell(r, 1).Value = row.ProductName;
                    ws.Cell(r, 2).Value = row.SoldQuantity;
                    ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 3).Value = row.Revenue;
                    ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(r, 4).Value = row.Profit;
                    ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                    r++;
                }

                // Итоговая строка
                ws.Cell(r, 1).Value = "ИТОГО";
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Cell(r, 2).Value = ProfitRows.Sum(x => x.SoldQuantity);
                ws.Cell(r, 2).Style.Font.Bold = true;
                ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(r, 3).Value = TotalSales;
                ws.Cell(r, 3).Style.Font.Bold = true;
                ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(r, 4).Value = TotalProfit;
                ws.Cell(r, 4).Style.Font.Bold = true;
                ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(r, 4).Style.Font.FontColor = XLColor.FromHtml("#107C10");

                if (ProfitRows.Count > 0)
                {
                    StyleTableData(ws.Range(headerStartRow + 1, 1, r - 1, 4));
                }

                ws.Range(r, 1, r, 4).Style.Border.TopBorder = XLBorderStyleValues.Medium;
                break;
            }
        }

        // Финальная настройка ширины колонок для всех листов
        foreach (var ws in wb.Worksheets)
        {
            foreach (var col in ws.Columns())
            {
                col.AdjustToContents();
                if (col.Width > 60) col.Width = 60; // Максимальная ширина
                if (col.Width < 12) col.Width = 12; // Минимальная ширина
            }
        }

        wb.SaveAs(stream);
    }
}