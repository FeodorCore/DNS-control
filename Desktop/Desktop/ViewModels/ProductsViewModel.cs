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

public partial class ProductsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _canDelete = true;

    // Полный список товаров из БД (используется как источник для фильтрации)
    private List<Product> _allProducts = new();

    public ProductsViewModel() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        var db = DatabaseService.Instance;
        _allProducts = await db.GetProductsAsync();
        Categories = new ObservableCollection<Category>(await db.GetCategoriesAsync());
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    async partial void OnSelectedProductChanged(Product? value)
    {
        if (value is null)
        {
            CanDelete = true;
            return;
        }
        CanDelete = !await DatabaseService.Instance.HasRelationsForProductAsync(value.ProductId);
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        IEnumerable<Product> filtered = _allProducts;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = _allProducts.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        Products = new ObservableCollection<Product>(filtered);
        // Если выбранный товар больше не виден — сбрасываем выбор
        if (SelectedProduct != null && !Products.Contains(SelectedProduct))
            SelectedProduct = Products.FirstOrDefault();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (Categories.Count == 0)
            return;
        var product = new Product
        {
            Name = "",
            CurrentPrice = 0,
            StockQuantity = 0,
            CategoryId = Categories.First().CategoryId
        };
        await DatabaseService.Instance.AddProductAsync(product);
        _allProducts.Add(product);
        ApplyFilter();
        SelectedProduct = product;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedProduct is null) return;
        await DatabaseService.Instance.DeleteProductAsync(SelectedProduct.ProductId);
        _allProducts.RemoveAll(p => p.ProductId == SelectedProduct.ProductId);
        var toRemove = SelectedProduct;
        SelectedProduct = null;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedProduct is null) return;
        await DatabaseService.Instance.UpdateProductAsync(SelectedProduct);
        var cat = Categories.FirstOrDefault(c => c.CategoryId == SelectedProduct.CategoryId);
        if (cat != null) SelectedProduct.CategoryName = cat.Name;
        var idxAll = _allProducts.FindIndex(p => p.ProductId == SelectedProduct.ProductId);
        if (idxAll >= 0) _allProducts[idxAll] = SelectedProduct;
        ApplyFilter();
    }
}