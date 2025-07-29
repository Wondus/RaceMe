using System.Security.Authentication;
using System.Threading.Tasks;
using Avalonia.Controls;
using Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Client.Views;

namespace Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Holds the current view to be shown beside the navbar
    /// </summary>
    [ObservableProperty] private UserControl _currentView;

    /// <summary>
    /// Holds data relevant for the current user
    /// null when not logged in
    /// </summary>
    [ObservableProperty] public User? currentUser;

    [ObservableProperty] public bool isAdmin;
    
    /// <summary>
    /// Viewmodels
    /// they are instantiated with a link to this MainWindowViewModel
    /// so they can call functions as SetupForUser()
    /// </summary>
    private ProfileViewModel ProfileVm { get; }
    private FeedViewModel FeedVm { get; set; }
    private LoginViewModel LoginVm { get; set; }
    private AdminViewModel AdminVm { get; set; }
    
    /// <summary>
    /// Create a MainWindowViewModel instance
    /// also creates view models for other screens and sets
    /// the current view to LoginView
    /// </summary>
    public MainWindowViewModel()
    {
        ProfileVm = new ProfileViewModel(this);
        FeedVm = new FeedViewModel(this);
        LoginVm = new LoginViewModel(this);
        CurrentView = new LoginView(LoginVm);
        AdminVm = new AdminViewModel(this);
        ShowLogin();
    }
    /// <summary>
    /// Sets relevant things to be ready for the current user
    /// </summary>
    /// <param name="user">User to set up for</param>
    public async void SetForUpUser(User user)
    {
        CurrentUser = user;
        ProfileVm.LoadUserImage();
        ProfileVm.Bio = user.Bio;
        IsAdmin = CurrentUser.isAdmin == 1;
        await FeedVm.InitializeFeed();
        await ShowFeed();
        await FeedVm.LoadNext();
    }
    /// <summary>
    /// Loads the login page
    /// </summary>
    /// <returns>Current view has been set to LoginView</returns>
    private Task ShowLogin()
    {
        CurrentView = new LoginView(LoginVm);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Loads the feed page
    /// </summary>
    /// <returns>Current view has been set to FeedView</returns>
    [RelayCommand]
    private Task ShowFeed()
    {
        if (CurrentUser is null)
        {
            return ShowLogin();
        }
        CurrentView = new FeedView(FeedVm);
        OnPropertyChanged(nameof(CurrentView));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads the profile page
    /// </summary>
    /// <returns>Current view has been set to ProfileView</returns>
    [RelayCommand]
    private Task ShowProfile()
    {
        if (CurrentUser is null)
        {
            return ShowLogin();
        }
        if (CurrentUser?.ImageBytes is { Length: > 0 })
            ProfileVm.LoadUserImage();
        CurrentView = new ProfileView(ProfileVm);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads the settings page
    /// </summary>
    /// <returns>Current view has been set to SettingsView</returns>
    [RelayCommand]
    private Task ShowSettings()
    {
        if (CurrentUser is null)
        {
            return ShowLogin();
        }
        CurrentView = new SettingsView(this);
        OnPropertyChanged(nameof(CurrentView));
        return Task.CompletedTask;
    }
    /// <summary>
    /// Loads the admin page
    /// </summary>
    /// <returns>Current view has been set to AdminView</returns>
    [RelayCommand]
    private async Task ShowAdmin()
    {
        if (CurrentUser is null)
        {
            await ShowLogin();
            return;
        }

        if (CurrentUser.isAdmin != 1)
        {
            throw new AuthenticationException("AdminView can only be accessed by admin users");
        }

        await AdminVm.InitializeUsers();
        await AdminVm.LoadNext();

        CurrentView = new AdminView(AdminVm);
        OnPropertyChanged(nameof(CurrentView));
    }
    /// <summary>
    /// Clears the current user and loads the login screen
    /// </summary>
    [RelayCommand]
    private Task Logout()
    {
        CurrentUser = null;
        IsAdmin = false;
        FeedVm = new FeedViewModel(this);
        ShowLogin();
        return Task.CompletedTask;
    }
}
