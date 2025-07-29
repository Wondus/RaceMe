using Avalonia.Controls;
using Client.ViewModels;

namespace Client.Views;

public partial class LoginView : UserControl
{
    public LoginView(LoginViewModel vm) 
    {
        InitializeComponent();
        DataContext = vm;
    }
}