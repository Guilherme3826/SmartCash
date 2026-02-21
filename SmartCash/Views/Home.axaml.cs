using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels;

namespace SmartCash.Views;

public partial class Home : UserControl
{
    public Home(HomeViewModel homeViewModel)
    {
        InitializeComponent();
        DataContext = homeViewModel;
    }
    public Home()
    {
        
    }
}