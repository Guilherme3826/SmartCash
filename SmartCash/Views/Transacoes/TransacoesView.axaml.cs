using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels.Transacoes;

namespace SmartCash.Views.Transacoes;

public partial class TransacoesView : UserControl
{
    public TransacoesView(TransacoesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    public TransacoesView()
    {
        InitializeComponent();
    }
}