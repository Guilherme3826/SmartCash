using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Consumiveis
{
    public partial class ConsumiveisViewModel : ObservableObject
    {
        private readonly IBaseRepository<ConsumiveisModel> _consumiveisRepository;
        private readonly IBaseRepository<CategoriaModel> _categoriaRepository;

        // Lista interna para manter todos os registros em memória e realizar o filtro localmente
        private List<ConsumiveisModel> _todosConsumiveis = new List<ConsumiveisModel>();

        [ObservableProperty]
        private bool _exibindoLista = true;

        [ObservableProperty]
        private object? _viewSubAtual;

        [ObservableProperty]
        private ObservableCollection<CategoriaModel> _categorias = new ObservableCollection<CategoriaModel>();

        [ObservableProperty]
        private CategoriaModel? _categoriaSelecionada;

        [ObservableProperty]
        private ObservableCollection<ConsumiveisModel> _consumiveis = new ObservableCollection<ConsumiveisModel>();

        [ObservableProperty]
        private ConsumiveisModel? _consumivelSelecionado;

        public ConsumiveisViewModel(
            IBaseRepository<ConsumiveisModel> consumiveisRepository,
            IBaseRepository<CategoriaModel> categoriaRepository)
        {
            _consumiveisRepository = consumiveisRepository;
            _categoriaRepository = categoriaRepository;

            _ = CarregarDadosAsync();
        }

        // Construtor vazio estritamente para o Design.DataContext do XAML não gerar erros no Visual Studio
        public ConsumiveisViewModel()
        {
        }

        public async Task CarregarDadosAsync()
        {
            // 1. Carrega as categorias do repositório
            var categoriasDb = await _categoriaRepository.GetAllAsync();

            // Cria uma categoria "falsa" para representar a opção de remover o filtro
            var categoriaTodas = new CategoriaModel
            {
                IdCategoria = 0,
                Nome = "Todas as Categorias"
            };

            var listaCategorias = new List<CategoriaModel> { categoriaTodas };
            listaCategorias.AddRange(categoriasDb);

            // 2. Carrega os consumíveis já com o Include da Categoria para o Binding da interface
            var consumiveisDb = await _consumiveisRepository.GetAllAsync();
            _todosConsumiveis = consumiveisDb;

            // 3. Atualiza as coleções observáveis que notificam a interface
            Categorias = new ObservableCollection<CategoriaModel>(listaCategorias);
            Consumiveis = new ObservableCollection<ConsumiveisModel>(_todosConsumiveis);
            CategoriaSelecionada = categoriaTodas;
        }

        [RelayCommand]
        private async Task ExcluirConsumivelAsync(ConsumiveisModel consumivel)
        {
            if (consumivel == null) return;
            try
            {            
                await _consumiveisRepository.DeleteAsync(consumivel.IdConsumivel);         
                Consumiveis.Remove(consumivel);               
            }
            catch (Exception ex)
            {               
                System.Diagnostics.Debug.WriteLine($"[ERRO AO EXCLUIR] {ex.Message}");
            }
        }

        // Método interceptador gerado pelo CommunityToolkit sempre que a propriedade CategoriaSelecionada é alterada
        partial void OnCategoriaSelecionadaChanged(CategoriaModel? value)
        {
            AplicarFiltro();
        }

        private void AplicarFiltro()
        {
            if (_todosConsumiveis == null || !_todosConsumiveis.Any())
            {
                return;
            }

            if (CategoriaSelecionada == null || CategoriaSelecionada.IdCategoria == 0)
            {
                // Restaura a lista completa se selecionar "Todas as Categorias"
                Consumiveis = new ObservableCollection<ConsumiveisModel>(_todosConsumiveis);
            }
            else
            {
                // Filtra os consumíveis baseando-se no ID da categoria selecionada no ComboBox
                var filtrados = _todosConsumiveis
                    .Where(c => c.IdCategoria == CategoriaSelecionada.IdCategoria)
                    .ToList();

                Consumiveis = new ObservableCollection<ConsumiveisModel>(filtrados);
            }
        }

        [RelayCommand]
        private async Task AdicionarAsync()
        {            
            var adicionarVm = App.ServiceProvider.GetRequiredService<AdicionarConsumivelViewModel>();
            ViewSubAtual = adicionarVm;
            ExibindoLista = false;
            await Task.CompletedTask;
        }
    }
}