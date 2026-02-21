using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.Views;
using SmartCash.Views.Categorias;
using System;
using System.Diagnostics;

namespace SmartCash.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private object? _viewAtual;

        [ObservableProperty]
        private bool _exibindoMenuPrincipal = true;

        [ObservableProperty]
        private bool _isPaneOpen; // Controla o menu lateral direito

        public MainViewModel()
        {
            ExibindoMenuPrincipal = true;
        }

        [RelayCommand]
        private void NavegarCategorias()
        {          
            var view = App.ServiceProvider.GetRequiredService<CategoriasView>();         
            ViewAtual = view;
            ExibindoMenuPrincipal = false;
        }

        [RelayCommand]
        private void Voltar()
        {
            ViewAtual = null;
            ExibindoMenuPrincipal = true; // Isso ativa a visibilidade da Dashboard no MainView.axaml 
        }

        [RelayCommand]
        private void NavegarProdutos() { /* Lógica futura */ }

        [RelayCommand]
        private void NavegarTransacoes() { /* Lógica futura */ }
    }
}