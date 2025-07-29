using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;
public partial class FeedViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;
    private List<User> _feedUsers = new();
    private int _currentIndex = -1;
    [ObservableProperty] private User _currentUser;
    
    [ObservableProperty]
    private Bitmap? _userImageBitmap;

    [ObservableProperty]
    private string _username = string.Empty;
    private User? _currentUserInFeed;

    [ObservableProperty]
    private string _bio = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _matchUsername = string.Empty;
    [ObservableProperty] private string _matchEmail = string.Empty;
    [ObservableProperty] private bool _isMatchPopupOpen;
    
    private void Display(User u)
    {
        _currentUserInFeed = u;
        Username = u.Username;
        Bio = u.Bio;
        Email = u.Email;
        if (u.ImageBytes != null) {
            using var ms = new MemoryStream(u.ImageBytes);
            UserImageBitmap = Bitmap.DecodeToWidth(ms, 500);
        } else {
            UserImageBitmap = null;
        }
    }

    
    public FeedViewModel(MainWindowViewModel main)
    {
        _main = main;
    }
    private readonly HashSet<int> _seenUserIds = new();
    public async Task InitializeFeed()
    {
        if (_main.CurrentUser is null)
        {
            throw new Exception("Current user is null");
        }
        var userId = _main.CurrentUser.Id;
        var resp = await Http.GetAsync($"/feed?userId={userId}");
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Could not load feed: {resp.StatusCode}");

        _feedUsers = await resp.Content
                         .ReadFromJsonAsync<List<User>>()
                     ?? new List<User>();

        _feedUsers = _feedUsers
            .Where(u => !_seenUserIds.Contains(u.Id))
            .ToList();

        _currentIndex = -1;
    }

    public async Task LoadNext()
    {
        if (_feedUsers.Count == 0)
            await InitializeFeed();

        _currentIndex++;
        if (_currentIndex < _feedUsers.Count)
        {
            Display(_feedUsers[_currentIndex]);
        }
        else
        {
            Username = "No more users.";
            UserImageBitmap = null;
            Bio = ""; 
            _currentUserInFeed = null;
        }
    }
    public async Task<bool> LikeCurrent()
    {
        if (_currentUserInFeed == null) return false;

        _seenUserIds.Add(_currentUserInFeed.Id);
        var matched = await SendMatch("like");
        return matched;
    }

    [RelayCommand]
    public async Task<bool> Report()
    {
        if (_currentUserInFeed == null) return false;

        _seenUserIds.Add(_currentUserInFeed.Id);
        
        var content = JsonContent.Create(_currentUserInFeed);
        
        await Http.PostAsync("/feed/report", content);
        await LoadNext();
        return true;
    }
    
    [RelayCommand]
    public async Task<bool> Dislike()
    {
        if (_currentUserInFeed == null) return false;

        _seenUserIds.Add(_currentUserInFeed.Id);
        await SendMatch("dislike");
        await LoadNext();
        return false;
    }
    
    private async Task<bool> SendMatch(string type)
    {
        if (_main.CurrentUser is null)
            throw new Exception("Current user is null");

        var content = JsonContent.Create(new
        {
            UserId = _main.CurrentUser.Id,
            SeenUserId = _currentUserInFeed!.Id,
            Liked = (type == "like")
        });

        var response = await Http.PostAsync("/feed/interact", content);
        if (!response.IsSuccessStatusCode)
            return false;

        var obj = await response.Content.ReadFromJsonAsync<JsonElement>();
        return obj.TryGetProperty("matched", out var m) && m.GetBoolean();
    }
}