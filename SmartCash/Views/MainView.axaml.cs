using Avalonia.Controls;
using Avalonia.Interactivity;
using SmartCash.ViewModels;

namespace SmartCash.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    // Método chamado quando a tela é carregada e anexada à interface
    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            // Inscreve-se no evento do botão físico de voltar do Android
            topLevel.BackRequested += TopLevel_BackRequested;
        }
    }

    // Método chamado quando a tela é destruída, importante para evitar vazamento de memória
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            // Remove a inscrição do evento
            topLevel.BackRequested -= TopLevel_BackRequested;
        }
    }

    private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // 1. Se o menu lateral direito estiver aberto, o botão voltar apenas fecha o menu
            if (viewModel.IsPaneOpen)
            {
                viewModel.IsPaneOpen = false;
                e.Handled = true; // Avisa ao Android que nós tratamos o botão voltar
                return;
            }

            // 2. Se estiver em uma sub-tela (como Categorias), executa o comando voltar
            if (!viewModel.ExibindoMenuPrincipal)
            {
                viewModel.VoltarCommand.Execute(null);
                e.Handled = true; // Avisa ao Android para não fechar o app
            }

            // 3. Se estiver no Dashboard (ExibindoMenuPrincipal == true) e o menu fechado,
            // o e.Handled continua false e o Android fecha o app normalmente.
        }
    }
}