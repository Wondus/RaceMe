using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Client.ViewModels;

namespace Client.Views;

public partial class ProfileView : UserControl
{
    public ProfileView(ProfileViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
    private async void UploadPhoto_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileViewModel vm)
        {
            await vm.UploadImageCommand.ExecuteAsync(this.GetVisualRoot() as Window);
        }
    }
}