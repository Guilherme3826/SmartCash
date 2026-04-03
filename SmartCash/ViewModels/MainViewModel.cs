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
                NavegarCategorias();

                var categoriasVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<CategoriasViewModel>(App.ServiceProvider);

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
            AtualizarEstadoNavegacao();
        }

        [RelayCommand]
        private void NavegarAjuda()
        {
            var view = App.ServiceProvider.GetRequiredService<AjudaView>();
            ViewAtual = view;
            AtualizarEstadoNavegacao();
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