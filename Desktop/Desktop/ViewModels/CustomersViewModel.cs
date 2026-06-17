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

public partial class CustomersViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _searchText = string.Empty;
    
    private List<Customer> _allCustomers = new();

    public CustomersViewModel() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        _allCustomers = await DatabaseService.Instance.GetCustomersAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        IEnumerable<Customer> filtered = _allCustomers;

        if (!string.IsNullOrEmpty(query))
        {
            filtered = _allCustomers.Where(c =>
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (c.Phone != null && c.Phone.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (c.Email != null && c.Email.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        Customers = new ObservableCollection<Customer>(filtered);
        if (SelectedCustomer != null && !Customers.Contains(SelectedCustomer))
            SelectedCustomer = Customers.FirstOrDefault();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var customer = new Customer { Name = "Новый покупатель" };
        await DatabaseService.Instance.AddCustomerAsync(customer);
        _allCustomers.Add(customer);
        ApplyFilter();
        SelectedCustomer = customer;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedCustomer is null) return;
        await DatabaseService.Instance.DeleteCustomerAsync(SelectedCustomer.CustomerId);
        _allCustomers.RemoveAll(c => c.CustomerId == SelectedCustomer.CustomerId);
        SelectedCustomer = null;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedCustomer is null) return;
        await DatabaseService.Instance.UpdateCustomerAsync(SelectedCustomer);
        var idx = _allCustomers.FindIndex(c => c.CustomerId == SelectedCustomer.CustomerId);
        if (idx >= 0) _allCustomers[idx] = SelectedCustomer;
        ApplyFilter();
    }
}