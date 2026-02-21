using Avalonia.Controls;
using Avalonia.Interactivity;
using SmartCash.ViewModels;
using SmartCash.ViewModels.Categorias; // Importante para o reconhecimento do tipo

namespace SmartCash.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            topLevel.BackRequested += TopLevel_BackRequested;
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            topLevel.BackRequested -= TopLevel_BackRequested;
        }
    }

    private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // 1. Se o menu de três pontos ou lateral estiver aberto, apenas fecha
            if (viewModel.IsPaneOpen)
            {
                viewModel.IsPaneOpen = false;
                e.Handled = true;
                return;
            }

            // 2. Se o usuário NÃO estiver na Dashboard
            if (!viewModel.ExibindoMenuPrincipal)
            {
                // REFERÊNCIA EXPLICITAMENTE À SUB-NAVEGAÇÃO:
                // Verificamos se a View sendo exibida no momento é a de Categorias
                // e se ela possui um formulário aberto (ExibindoLista == false)
                if (viewModel.ViewAtual is Control { DataContext: CategoriasViewModel catVm } && !catVm.ExibindoLista)
                {
                    // Em vez de voltar para a MainView, apenas manda a tela de categorias fechar o formulário
                    catVm.ExibindoLista = true;
                    catVm.ViewSubAtual = null;

                    e.Handled = true; // Avisa ao Android que o evento foi tratado aqui
                    return;
                }

                // 3. Se a tela de categorias já estiver na lista, ou for outra tela, volta para a Dashboard
                viewModel.VoltarCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}