using Avalonia.Controls;
using Avalonia.Interactivity;
using Client.ViewModels;

namespace Client.Views
{
    public partial class FeedView : UserControl
    {
        private readonly FeedViewModel _vm;

        public FeedView(FeedViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
        }

        private async void LikeClick(object? sender, RoutedEventArgs e)
        {
            var matched = await _vm.LikeCurrent();
            if (matched)
            {
                _vm.MatchUsername = _vm.Username;
                _vm.MatchEmail = _vm.Email;
                _vm.IsMatchPopupOpen = true;
            }

            await _vm.LoadNext();
        }

        private void ClosePopup(object? sender, RoutedEventArgs e)
        {
            _vm.IsMatchPopupOpen = false;
        }
    }
}