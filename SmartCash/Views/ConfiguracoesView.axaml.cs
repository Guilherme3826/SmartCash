using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels;

namespace SmartCash.Views;

public partial class ConfiguracoesView : UserControl
{
    public ConfiguracoesView(ConfiguracoesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    public ConfiguracoesView()
    {
        InitializeComponent();
    }
}