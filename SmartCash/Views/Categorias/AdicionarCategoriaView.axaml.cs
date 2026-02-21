using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SmartCash.ViewModels.Categorias;

namespace SmartCash.Views.Categorias;

public partial class AdicionarCategoriaView : UserControl
{
    public AdicionarCategoriaView(AdicionarCategoriaViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public AdicionarCategoriaView()
    {
        InitializeComponent();
    }

    // Método chamado quando a View é adicionada à tela
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            // Inscreve-se no evento do botão voltar do Android
            topLevel.BackRequested += TopLevel_BackRequested;
        }
    }

    // Método chamado quando a View é removida da tela
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            // Remove a inscrição para liberar memória
            topLevel.BackRequested -= TopLevel_BackRequested;
        }
    }

    // Lógica que intercepta o botão voltar
    private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AdicionarCategoriaViewModel viewModel)
        {
            // Executa o comando de cancelar que já criamos na ViewModel
            // Este comando deve setar ExibindoLista = true na ViewModel pai
            if (viewModel.CancelarCommand.CanExecute(null))
            {
                viewModel.CancelarCommand.Execute(null);

                // Avisa ao Android que nós já tratamos o evento e ele não deve fechar o app
                e.Handled = true;
            }
        }
    }
}