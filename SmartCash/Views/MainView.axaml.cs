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
            // 1. Prioridade: Se o menu lateral estiver aberto, fecha e encerra
            if (viewModel.IsPaneOpen)
            {
                viewModel.IsPaneOpen = false;
                e.Handled = true;
                return;
            }

            // 2. Se já estamos na Dashboard (Menu Principal), deixa o Android 
            // decidir (provavelmente minimizar o app)
            if (viewModel.ExibindoMenuPrincipal)
            {
                return;
            }

            // 3. LÓGICA DE INTERCEPTAÇÃO DE SUB-VIEWS
            // Se a View atual for uma tela que possui uma "Sub-View" aberta (ex: formulário de adição),
            // nós NÃO podemos executar o VoltarCommand da MainViewModel agora.

            if (viewModel.ViewAtual is Control { DataContext: object subVm })
            {
                // Verifica dinamicamente se a ViewModel da tela atual tem uma sub-view aberta
                // Usamos 'dynamic' para evitar espalhar IFs para cada tipo de ViewModel (Categorias, Consumiveis, Transacoes)
                try
                {
                    dynamic vm = subVm;
                    if (vm.ExibindoLista == false)
                    {
                        // Se ExibindoLista for false, significa que há um formulário aberto.
                        // O próprio formulário (AdicionarConsumivelView, etc) já tem seu 
                        // código de interceptação que vai disparar e tratar o evento.
                        // Portanto, a MainView deve ficar quieta.
                        return;
                    }
                }
                catch { /* A VM não possui a propriedade ExibindoLista, segue o fluxo */ }
            }

            // 4. Se chegou aqui, significa que estamos em uma tela secundária (ex: Lista de Consumíveis)
            // mas não há nenhum formulário de adição aberto. Então voltamos para a Dashboard.
            viewModel.VoltarCommand.Execute(null);
            e.Handled = true;
        }
    }
}