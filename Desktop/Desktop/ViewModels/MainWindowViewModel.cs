using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentView;

    public ObservableCollection<NavigationItem> MenuItems { get; } = new()
    {
        new NavigationItem("Товары", typeof(ProductsViewModel)),
        new NavigationItem("Категории", typeof(CategoriesViewModel)),
        new NavigationItem("Поставщики", typeof(SuppliersViewModel)),
        new NavigationItem("Поставки", typeof(SuppliesViewModel)),
        new NavigationItem("Продажи", typeof(SalesViewModel)),
        new NavigationItem("Отчёты", typeof(ReportsViewModel)),
    };

    [RelayCommand]
    private void Navigate(NavigationItem? item)
    {
        if (item?.ViewModelType is not null)
            CurrentView = (ViewModelBase)Activator.CreateInstance(item.ViewModelType)!;
    }
}

public class NavigationItem
{
    public string Title { get; }
    public Type ViewModelType { get; }

    public NavigationItem(string title, Type viewModelType)
    {
        Title = title;
        ViewModelType = viewModelType;
    }
}