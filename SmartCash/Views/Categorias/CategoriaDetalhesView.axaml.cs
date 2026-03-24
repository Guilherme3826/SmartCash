using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels.Categorias;

namespace SmartCash.Views.Categorias;

public partial class CategoriaDetalhesView : UserControl
{
    public CategoriaDetalhesView(CategoriaDetalhesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    public CategoriaDetalhesView()
    {
        InitializeComponent();
    }
}