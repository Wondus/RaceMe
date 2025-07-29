using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Client.ViewModels;
using ReactiveUI;

namespace Client.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView(MainWindowViewModel main)
        {
            InitializeComponent();
            DataContext = main;
        }
    }
}
