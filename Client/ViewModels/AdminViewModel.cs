using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class AdminViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;
    private List<User> _usersToModerate = new();
    private int _currentIndex = -1;

    [ObservableProperty] private User? _currentUserInFeed;
    [ObservableProperty] private Bitmap? _userImageBitmap;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _bio = string.Empty;

    public AdminViewModel(MainWindowViewModel main)
    {
        _main = main;
    }

    private void Display(User u)
    {
        _currentUserInFeed = u;
        Username = u.Username;
        Bio = u.Bio;
        if (u.ImageBytes is { Length: > 0 })
        {
            try
            {
                using var ms = new MemoryStream(u.ImageBytes);
                UserImageBitmap = Bitmap.DecodeToWidth(ms, 500);
            }
            catch
            {
                UserImageBitmap = null;
            }
        }
        else
        {
            UserImageBitmap = null;
        }
    }

    public async Task InitializeUsers()
    {
        if (_main.CurrentUser == null)
            throw new Exception("Current user is null");

        var resp = await Http.GetAsync("/admin/users-to-moderate");
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Could not load users: {resp.StatusCode}");

        _usersToModerate = await resp.Content.ReadFromJsonAsync<List<User>>() 
                             ?? new List<User>();
        _currentIndex = -1;
    }

    public async Task LoadNext()
    {
        _currentIndex++;
        if (_usersToModerate.Count == 0 || _currentIndex >= _usersToModerate.Count)
        {
            Username = "No more users.";
            Bio = "";
            UserImageBitmap = null;
            _currentUserInFeed = null;
            return;
        }
        Display(_usersToModerate[_currentIndex]);
    }

    [RelayCommand]
    public async Task Ban()
    {
        if (_currentUserInFeed == null) return;

        var userId = _currentUserInFeed.Id;
        var response = await Http.DeleteAsync($"/admin/ban/{userId}");
        if (!response.IsSuccessStatusCode)
        {
            // Optionally handle error
            return;
        }

        await LoadNext();
    }

    [RelayCommand]
    public async Task Unban()
    {
        if (_currentUserInFeed == null) return;

        var userId = _currentUserInFeed.Id;
        var response = await Http.DeleteAsync($"/admin/unban/{userId}");
        if (!response.IsSuccessStatusCode)
        {
            // Optionally handle error
            return;
        }

        await LoadNext();
    }
}