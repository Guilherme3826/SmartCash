using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.Views;
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
        private void AlternarMenu()
        {
            IsPaneOpen = !IsPaneOpen;
        }

        [RelayCommand]
        private void NavegarCategorias()
        {
            IsPaneOpen = false; // Fecha o menu ao navegar
            var view = App.ServiceProvider.GetRequiredService<Categorias>();         
            ViewAtual = view;
            ExibindoMenuPrincipal = false;
        }

        [RelayCommand]
        private void Voltar()
        {
            ViewAtual = null;
            ExibindoMenuPrincipal = true; // Isso ativa a visibilidade da Dashboard no MainView.axaml
            IsPaneOpen = false;
           
        }

        [RelayCommand]
        private void NavegarProdutos() { /* Lógica futura */ }

        [RelayCommand]
        private void NavegarTransacoes() { /* Lógica futura */ }
    }
}