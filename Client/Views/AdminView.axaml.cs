using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Client.ViewModels;

namespace Client.Views;

public partial class AdminView : UserControl
{
    public AdminView(AdminViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        Loaded += async (_, _) =>
        {
            await vm.InitializeUsers();
            await vm.LoadNext();
        };
    }
}
