using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;

namespace Desktop.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    [ObservableProperty] private string _selectedReport = "Продажи по дням";

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

    [ObservableProperty] private string _selectedCategory = "Все";

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
        var db = DatabaseService.Instance;
        DataTable? dt = SelectedReport switch
        {
            "Товары на складе" => await db.GetStockReportAsync(),
            "Продажи по дням" => await db.GetSalesByDayReportAsync(
                DateFrom?.DateTime ?? DateTime.MinValue,
                DateTo?.DateTime ?? DateTime.MaxValue,
                SelectedCategory),
            "Прибыль по товарам" => await db.GetProfitByProductReportAsync(
                DateFrom?.DateTime ?? DateTime.MinValue,
                DateTo?.DateTime ?? DateTime.MaxValue,
                SelectedCategory),
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
}