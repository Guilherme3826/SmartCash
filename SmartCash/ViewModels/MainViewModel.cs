using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.Mensageiros;
using SmartCash.ViewModels.Categorias;
using SmartCash.Views;
using SmartCash.Views.Categorias;
using SmartCash.Views.Consumiveis;
using SmartCash.Views.Dashboard;
using SmartCash.Views.Transacoes;
using System.Threading.Tasks;

namespace SmartCash.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty] private object? _viewAtual;
        [ObservableProperty] private bool _isCategoriasActive;
        [ObservableProperty] private bool _isConsumiveisActive;
        [ObservableProperty] private bool _isTransacoesActive;
        [ObservableProperty] private bool _isDashboardActive;

        public MainViewModel()
        {
            NavegarDashboard();

            WeakReferenceMessenger.Default.Register<NavegarParaCategoriaDetalhesGlobalMessage>(this, (r, m) =>
            {
                // 1. Muda para a aba de Categorias (substitua pelo seu comando exato se for diferente)
                NavegarCategorias();

                // 2. Pede a CategoriasViewModel ao sistema. Se ela não existia, é criada agora!
                var categoriasVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<CategoriasViewModel>(App.ServiceProvider);

                // 3. Executamos de forma assíncrona com um atraso mínimo de 50 milissegundos.
                // Isso garante que a animação/troca da aba do Avalonia termine antes de forçarmos a exibição dos detalhes,
                // evitando que a navegação do menu "atropele" a abertura da sub-tela.
                Task.Delay(50).ContinueWith(_ =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        categoriasVm.AbrirDetalhesExternamente(m.IdCategoria, m.PeriodoFiltro);
                    });
                });
            });
        }

        [RelayCommand]
        private void NavegarCategorias()
        {
            var view = App.ServiceProvider.GetRequiredService<CategoriasView>();
            ViewAtual = view;
            AtualizarEstadoNavegacao(categorias: true);
        }

        [RelayCommand]
        private void NavegarConsumiveis()
        {
            var view = App.ServiceProvider.GetRequiredService<ConsumiveisView>();
            ViewAtual = view;
            AtualizarEstadoNavegacao(consumiveis: true);
        }

        [RelayCommand]
        private void NavegarTransacoes()
        {
            var view = App.ServiceProvider.GetRequiredService<TransacoesView>();
            ViewAtual = view;
            AtualizarEstadoNavegacao(transacoes: true);
        }

        [RelayCommand]
        private void NavegarConfiguracoes()
        {
            var view = App.ServiceProvider.GetRequiredService<ConfiguracoesView>();
            ViewAtual = view;
            AtualizarEstadoNavegacao(); // Deixa todos falsos, pois a tela não está na BottomNav
        }

        [RelayCommand]
        private void NavegarDashboard()
        {
            var view = App.ServiceProvider.GetRequiredService<DashboardView>();
            ViewAtual = view;
            AtualizarEstadoNavegacao(dashboard: true);
        }

        [RelayCommand]
        private void Voltar()
        {
            NavegarDashboard();
        }

        private void AtualizarEstadoNavegacao(bool categorias = false, bool consumiveis = false, bool transacoes = false, bool dashboard = false)
        {
            IsCategoriasActive = categorias;
            IsConsumiveisActive = consumiveis;
            IsTransacoesActive = transacoes;
            IsDashboardActive = dashboard;
        }
    }
}