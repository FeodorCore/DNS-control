using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase? _currentView;

    [ObservableProperty] private NavigationItem? selectedMenuItem;

    [ObservableProperty] private string _currentTitle = "Главная";


    public ObservableCollection<NavigationItem> MenuItems { get; } = new()
    {
        new NavigationItem("Товары", typeof(ProductsViewModel)),
        new NavigationItem("Категории", typeof(CategoriesViewModel)),
        new NavigationItem("Поставщики", typeof(SuppliersViewModel)),
        new NavigationItem("Поставки", typeof(SuppliesViewModel)),
        new NavigationItem("Продажи", typeof(SalesViewModel)),
        new NavigationItem("Отчёты", typeof(ReportsViewModel)),
    };

    public MainWindowViewModel()
    {
        if (MenuItems.Count > 0) SelectedMenuItem = MenuItems[0];
    }

    partial void OnSelectedMenuItemChanged(NavigationItem? value)
    {
        if (value is not null)
        {
            CurrentTitle = value.Title;
            
            if (value.ViewModelType is not null)
            {
                CurrentView = (ViewModelBase)Activator.CreateInstance(value.ViewModelType)!;
            }
        }
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