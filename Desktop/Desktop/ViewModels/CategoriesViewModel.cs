using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Desktop.Models;

namespace Desktop.ViewModels;

public partial class CategoriesViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private string _searchText = string.Empty;

    private List<Category> _allCategories = new();

    public CategoriesViewModel() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        _allCategories = await DatabaseService.Instance.GetCategoriesAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        IEnumerable<Category> filtered = _allCategories;

        if (!string.IsNullOrEmpty(query))
        {
            filtered = _allCategories.Where(c =>
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        Categories = new ObservableCollection<Category>(filtered);

        if (SelectedCategory != null && !Categories.Contains(SelectedCategory))
            SelectedCategory = Categories.FirstOrDefault();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var cat = new Category { Name = "Новая категория" };
        await DatabaseService.Instance.AddCategoryAsync(cat);
        _allCategories.Add(cat);
        ApplyFilter();
        SelectedCategory = cat;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedCategory is null) return;
        await DatabaseService.Instance.DeleteCategoryAsync(SelectedCategory.CategoryId);
        _allCategories.RemoveAll(c => c.CategoryId == SelectedCategory.CategoryId);
        SelectedCategory = null;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedCategory is null) return;
        await DatabaseService.Instance.UpdateCategoryAsync(SelectedCategory);
        var idx = _allCategories.FindIndex(c => c.CategoryId == SelectedCategory.CategoryId);
        if (idx >= 0) _allCategories[idx] = SelectedCategory;
        ApplyFilter();
    }
}