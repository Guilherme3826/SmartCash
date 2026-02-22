using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels.Consumiveis;

namespace SmartCash;

public partial class ConsumiveisView : UserControl
{
    public ConsumiveisView(ConsumiveisViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    public ConsumiveisView()
    {
        InitializeComponent();
    }
}