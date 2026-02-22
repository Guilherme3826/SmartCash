using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SmartCash.ViewModels.Consumiveis;

namespace SmartCash.Views.Consumiveis;

public partial class AdicionarConsumivelView : UserControl
{
    public AdicionarConsumivelView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Obtém o TopLevel (Janela/Activity principal) para ouvir eventos de hardware
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            // Remove antes de adicionar para prevenir múltiplas assinaturas acidentais
            topLevel.BackRequested -= TopLevel_BackRequested;
            topLevel.BackRequested += TopLevel_BackRequested;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            // Crucial para que o botão voltar volte a funcionar normalmente nas outras telas
            topLevel.BackRequested -= TopLevel_BackRequested;
        }

        // Limpa o DataContext para garantir que a View possa ser coletada pelo GC e evitar erros de Visual Parent
        DataContext = null;
    }

    private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
    {
        // Verifica se o DataContext é a ViewModel correta
        if (DataContext is AdicionarConsumivelViewModel vm)
        {
            // Se o comando puder ser executado (lógica de validação da VM)
            if (vm.VoltarCommand.CanExecute(null))
            {
                // Marca o evento como 'Handled' para o Android não fechar o app
                e.Handled = true;

                // Executa a lógica de fechar a sub-view e voltar para a lista
                vm.VoltarCommand.Execute(null);
            }
        }
    }
}