using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels.Dashboard;

namespace SmartCash.Views.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    public DashboardView()
    {
        InitializeComponent();
    }
}