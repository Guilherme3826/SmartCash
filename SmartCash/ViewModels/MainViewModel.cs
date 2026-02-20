using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.Views;
using System;

namespace SmartCash.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private object? _viewAtual;

        [ObservableProperty]
        private bool _exibindoMenuPrincipal = true;

        public MainViewModel()
        {
            ExibindoMenuPrincipal = true;
        }

        [RelayCommand]
        private void NavegarCategorias()
        {
            // Busca a CategoriaView resolvida pelo Injetor de Dependência
            // Certifique-se de registrar a View e a ViewModel no App.axaml.cs
            //var view = App.ServiceProvider.GetRequiredService<CategoriaView>();
            //view.DataContext = App.ServiceProvider.GetRequiredService<CategoriaViewModel>();

            //ViewAtual = view;
            ExibindoMenuPrincipal = false;
        }

        [RelayCommand]
        private void Voltar()
        {
            ViewAtual = null;
            ExibindoMenuPrincipal = true;
        }

        [RelayCommand]
        private void NavegarProdutos() { /* Implementação futura */ }

        [RelayCommand]
        private void NavegarTransacoes() { /* Implementação futura */ }
    }
}