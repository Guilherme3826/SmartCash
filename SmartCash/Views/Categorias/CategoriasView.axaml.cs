using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels;
using SmartCash.ViewModels.Categorias;

namespace SmartCash.Views.Categorias;

public partial class CategoriasView : UserControl
{
    public CategoriasView(CategoriasViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    public CategoriasView()
    {
        InitializeComponent();
    }
}