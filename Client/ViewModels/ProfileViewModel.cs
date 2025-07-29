using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap? _userImageBitmap;

    [ObservableProperty] private string _bio;
    [ObservableProperty] private string _newUsername;
    [ObservableProperty] private string _newPassword;
    [ObservableProperty] private string _newPasswordAgain;
    
    [ObservableProperty]
    private string _errorMessage;

    private readonly MainWindowViewModel _main;

    public ProfileViewModel(MainWindowViewModel main)
    {
        NewUsername = string.Empty;
        NewPassword = string.Empty;
        NewPasswordAgain = string.Empty;
        ErrorMessage = string.Empty;
        Bio = string.Empty;
        _main = main;

        if (_main.CurrentUser?.ImageBytes is { Length: > 0 })
        {
            LoadUserImage();
        }
    }

    public void LoadUserImage()
    {
        if (_main.CurrentUser?.ImageBytes is not { Length: > 0 })
            return;

        using var ms = new MemoryStream(_main.CurrentUser.ImageBytes);
        UserImageBitmap = Bitmap.DecodeToWidth(ms, 512);
    }

    [RelayCommand]
    public async Task ChangeUsername(Window parentWindow)
    {
        if (_main.CurrentUser is null)
        {
            throw new Exception("Current user is null");
        }
        
        await Http.PutAsJsonAsync($"/update/username?userId={_main.CurrentUser.Id}", new { NewUsername });
    }
    
    [RelayCommand]
    public async Task ChangePassword(Window parentWindow)
    {
        if (_main.CurrentUser is null)
        {
            throw new Exception("Current user is null");
        }
        
        if (!NewPassword.Equals(NewPasswordAgain))
        {
            return;
        }
        
        await Http.PutAsJsonAsync($"/update/password?userId={_main.CurrentUser.Id}", new { NewPassword });
    }

    [RelayCommand]
    public async Task UploadImage(Window parentWindow)
    {
        if (_main.CurrentUser is null)
        {
            throw new Exception("Current user is null");
        }
        var dialog = new OpenFileDialog
        {
            Title = "Select an image",
            Filters =
            {
                new FileDialogFilter { Name = "Image Files", Extensions = { "jpg", "jpeg", "png" } }
            },
            AllowMultiple = false
        };

        var uploadResult = await dialog.ShowAsync(parentWindow);
        if (uploadResult != null && uploadResult.Length > 0)
        {
            var filePath = uploadResult[0];

            using var stream = File.OpenRead(filePath);
            UserImageBitmap = await Task.Run(() => Bitmap.DecodeToWidth(stream, 512));

            _main.CurrentUser.ImageBytes = await File.ReadAllBytesAsync(filePath);

            await SaveImageToDatabaseAsync();
        }
    }

    private async Task SaveImageToDatabaseAsync()
    {
        if (_main.CurrentUser is null || _main.CurrentUser.Id == 0 || _main.CurrentUser.ImageBytes is null)
        {
            ErrorMessage = "User not logged in or image not available";
        }

        try
        {
            using var content = new MultipartFormDataContent();
            var byteArrayContent = new ByteArrayContent(_main.CurrentUser.ImageBytes);
            content.Add(byteArrayContent, "photoFile", "profile.jpg");

            var response = await Http.PutAsync($"/update/photo?userId={_main.CurrentUser.Id}", content);

            if (response.IsSuccessStatusCode)
            {
                ErrorMessage = string.Empty;
            }else
            {
                ErrorMessage = "Failed to upload image to database";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Upload error: {ex.Message}";
        }
        
    }

    [RelayCommand]
    private async Task EditBio()
    {
        if (_main.CurrentUser is null)
        {
            throw new Exception("Current user is null");
        }

        _main.CurrentUser.Bio = Bio;
        await Http.PutAsJsonAsync($"/update/bio?userId={_main.CurrentUser.Id}", new { newBio = Bio });
        
    }
}
