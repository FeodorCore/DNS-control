using Avalonia.Controls;
using Avalonia.Interactivity;
using Desktop.ViewModels;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        menuList.SelectionChanged += OnMenuSelectionChanged;
    }

    private void OnMenuSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && menuList.SelectedItem is NavigationItem navItem)
        {
            vm.NavigateCommand.Execute(navItem);
        }
    }
}