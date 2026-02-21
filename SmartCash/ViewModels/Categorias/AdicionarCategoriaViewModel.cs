using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Repositories;
using SmartCash.EfCore.Models;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Categorias
{
    public partial class AdicionarCategoriaViewModel : ObservableObject
    {
        private IBaseRepository<CategoriaModel> _categoriaRepository;

        // Referência para a ViewModel pai para controlar a navegação e atualização
        private readonly CategoriasViewModel _parentViewModel;

        [ObservableProperty]
        private string _novoNome = string.Empty;

        [ObservableProperty]
        private string? _iconeSelecionado;

        public ObservableCollection<string> IconesDisponiveis { get; } = new();

        // O construtor agora recebe tanto o repositório quanto a ViewModel pai
        public AdicionarCategoriaViewModel(
            IBaseRepository<CategoriaModel> categoriaRepository,
            CategoriasViewModel parentViewModel)
        {
            _categoriaRepository = categoriaRepository;
            _parentViewModel = parentViewModel;

            CarregarIcones();
            IconeSelecionado = "fa-solid fa-tags";
        }

        private void CarregarIcones()
        {
            var icons = new List<string>
            {
                "fa-solid fa-tags", "fa-solid fa-cart-shopping", "fa-solid fa-bag-shopping",
                "fa-solid fa-basket-shopping", "fa-solid fa-store", "fa-solid fa-credit-card",
                "fa-solid fa-utensils", "fa-solid fa-burger", "fa-solid fa-pizza-slice",
                "fa-solid fa-ice-cream", "fa-solid fa-coffee", "fa-solid fa-beer-mug-empty",
                "fa-solid fa-car", "fa-solid fa-bus", "fa-solid fa-plane", "fa-solid fa-train",
                "fa-solid fa-gas-pump", "fa-solid fa-motorcycle", "fa-solid fa-bicycle",
                "fa-solid fa-house", "fa-solid fa-lightbulb", "fa-solid fa-faucet",
                "fa-solid fa-plug", "fa-solid fa-wifi", "fa-solid fa-phone",
                "fa-solid fa-gamepad", "fa-solid fa-film", "fa-solid fa-music",
                "fa-solid fa-tv", "fa-solid fa-camera", "fa-solid fa-theater-masks",
                "fa-solid fa-heart-pulse", "fa-solid fa-stethoscope", "fa-solid fa-pills",
                "fa-solid fa-dumbbell", "fa-solid fa-hospital", "fa-solid fa-spa",
                "fa-solid fa-graduation-cap", "fa-solid fa-book", "fa-solid fa-laptop",
                "fa-solid fa-briefcase", "fa-solid fa-pen-nib", "fa-solid fa-microchip",
                "fa-solid fa-wallet", "fa-solid fa-money-bill-wave", "fa-solid fa-coins",
                "fa-solid fa-piggy-bank", "fa-solid fa-chart-line", "fa-solid fa-gift",
                "fa-solid fa-shirt", "fa-solid fa-paw", "fa-solid fa-tree", "fa-solid fa-umbrella-beach"
            };

            foreach (var icon in icons)
            {
                IconesDisponiveis.Add(icon);
            }
        }

        [RelayCommand]
        private async Task Salvar()
        {
            // Validação simples
            if (!string.IsNullOrWhiteSpace(NovoNome) && !string.IsNullOrEmpty(IconeSelecionado))
            {
                var categoria = new CategoriaModel()
                {
                    Nome = NovoNome,
                    IconeApresentacao = IconeSelecionado
                };

                // 1. Salva no banco de dados SQLite
                await _categoriaRepository.AddAsync(categoria);

                // 2. AQUI ESTÁ A LÓGICA DE RETORNO E ATUALIZAÇÃO:
                // Avisa a tela de categorias para voltar a exibir a lista
                _parentViewModel.ExibindoLista = true;

                // Remove a View de adição da memória
                _parentViewModel.ViewSubAtual = null;

                // 3. Atualiza a lista de categorias para incluir a nova que acabamos de criar
                await _parentViewModel.CarregarCategorias();
            }
        }

        [RelayCommand]
        private void Cancelar()
        {
            // Apenas volta para a lista sem salvar e sem recarregar dados
            _parentViewModel.ExibindoLista = true;
            _parentViewModel.ViewSubAtual = null;
        }
    }
}