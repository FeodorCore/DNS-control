using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Desktop.Models;

namespace Desktop.ViewModels;

public partial class CategoriesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();
    [ObservableProperty]
    private Category? _selectedCategory;

    public CategoriesViewModel() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        var list = await DatabaseService.Instance.GetCategoriesAsync();
        Categories = new ObservableCollection<Category>(list);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var cat = new Category { Name = "Новая категория" };
        await DatabaseService.Instance.AddCategoryAsync(cat);
        Categories.Add(cat);
        SelectedCategory = cat;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedCategory is null) return;
        await DatabaseService.Instance.DeleteCategoryAsync(SelectedCategory.CategoryId);
        Categories.Remove(SelectedCategory);
        SelectedCategory = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedCategory is null) return;
        await DatabaseService.Instance.UpdateCategoryAsync(SelectedCategory);
        var idx = Categories.IndexOf(SelectedCategory);
        if (idx >= 0) Categories[idx] = SelectedCategory;
    }
}