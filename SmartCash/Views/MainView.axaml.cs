using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SmartCash.ViewModels;

namespace SmartCash.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                // Inscreve no evento nativo de voltar do sistema (ex: botão voltar do Android)
                topLevel.BackRequested += TopLevel_BackRequested;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                // Remove a inscrição para evitar vazamento de memória
                topLevel.BackRequested -= TopLevel_BackRequested;
            }
        }

        private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                // Verifica se NÃO estamos no Dashboard
                if (!viewModel.IsDashboardActive)
                {
                    // Intercepta a ação nativa para impedir que o aplicativo feche
                    e.Handled = true;

                    // Executa o comando de voltar da ViewModel (que leva ao Dashboard)
                    viewModel.VoltarCommand.Execute(null);
                }
                // Se já estiver no Dashboard, o e.Handled continuará falso e o Android fechará o aplicativo normalmente.
            }
        }
    }
}