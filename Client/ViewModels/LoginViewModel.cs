using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Client.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty] private string _loginUsername;
    [ObservableProperty] private string _registerUsername;
    [ObservableProperty] private string _email;
    [ObservableProperty] private string _loginPassword;
    [ObservableProperty] private string _registerPassword;
    [ObservableProperty] private string _errorMessage;

    private readonly MainWindowViewModel _main;

    public LoginViewModel(MainWindowViewModel main)
    {
        _loginUsername = string.Empty;
        _registerUsername = string.Empty;
        _email = string.Empty;
        _loginPassword = string.Empty;
        _registerPassword = string.Empty;
        _errorMessage = string.Empty;

        _main = main;
    }

    [RelayCommand]
    private async Task Login()
    {
        var result = await Http.PostAsJsonAsync("/login", new { Username = LoginUsername, Password = LoginPassword });
        if (result.IsSuccessStatusCode)
        {
            var user = await result.Content.ReadFromJsonAsync<User>();
            if (user != null)
            {
                Console.WriteLine(user.Username);
                _main.SetForUpUser(user);
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to parse user info.";
            }
        }
        else
        {
            ErrorMessage = "Invalid login.";
        }
    }

    [RelayCommand]
    private async Task Register()
    {
        var result = await Http.PostAsJsonAsync("/register", new { Username = RegisterUsername, Email, Password = RegisterPassword });
        if (result.IsSuccessStatusCode)
        {
            ErrorMessage = string.Empty;
        }
        else
        {
            ErrorMessage = "One of the fields is empty, or username/email is taken";
        }
    }
}