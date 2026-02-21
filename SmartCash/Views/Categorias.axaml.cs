using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels;

namespace SmartCash.Views;

public partial class Categorias : UserControl
{
    public Categorias(CategoriasViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    public Categorias()
    {
        InitializeComponent();
    }
}