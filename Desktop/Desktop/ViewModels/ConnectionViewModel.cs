using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Data;
using Desktop.Models;
using Desktop.Services;

namespace Desktop.ViewModels;

public partial class ConnectionViewModel : ViewModelBase
{
    [ObservableProperty] private string _host = "localhost";
    [ObservableProperty] private string _port = "5432";
    [ObservableProperty] private string _database = "postgres";
    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private bool _isConnecting;

    public bool IsConnected { get; private set; }
    public event Action? ConnectionSucceeded;

    public ConnectionViewModel()
    {
        var settings = SettingsService.Load();
        Host = settings.Host;
        Port = settings.Port.ToString();
        Database = settings.Database;
        Username = settings.Username;
        Password = settings.Password;
    }

    private string BuildConnectionString()
    {
        if (!int.TryParse(Port, out var port))
            port = 5432;
        return $"Host={Host};Port={port};Database={Database};Username={Username};Password={Password}";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(Host))
        {
            ErrorMessage = "Укажите хост.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Database))
        {
            ErrorMessage = "Укажите базу данных.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Укажите имя пользователя.";
            return;
        }

        IsConnecting = true;
        try
        {
            var cs = BuildConnectionString();
            var ok = await DatabaseService.TestConnectionAsync(cs);
            if (ok)
                SuccessMessage = "✓ Подключение успешно!";
            else
                ErrorMessage = "Не удалось подключиться. Проверьте параметры.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(Host))
        {
            ErrorMessage = "Укажите хост.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Database))
        {
            ErrorMessage = "Укажите базу данных.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Укажите имя пользователя.";
            return;
        }

        IsConnecting = true;
        try
        {
            var cs = BuildConnectionString();
            var ok = await DatabaseService.TestConnectionAsync(cs);
            if (!ok)
            {
                ErrorMessage = "Не удалось подключиться. Проверьте параметры и попробуйте снова.";
                return;
            }

            // АВТОМАТИЧЕСКОЕ СОЗДАНИЕ ТАБЛИЦ, если их нет
            await DatabaseService.InitializeSchemaAsync(cs);

            // Инициализируем DatabaseService
            DatabaseService.Initialize(cs);

            // Сохраняем настройки
            if (!int.TryParse(Port, out var port))
                port = 5432;
            
            SettingsService.Save(new ConnectionSettings
            {
                Host = Host,
                Port = port,
                Database = Database,
                Username = Username,
                Password = Password
            });

            IsConnected = true;
            ConnectionSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка инициализации БД: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }
}