using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Consumiveis
{
    public partial class AdicionarConsumivelViewModel : ObservableObject
    {
        private readonly IBaseRepository<ConsumiveisModel> _repository;
        private readonly IBaseRepository<CategoriaModel> _catRepository;
        private readonly ConsumiveisViewModel _parent;
        private readonly ConsumiveisModel? _itemParaEdicao;

        [ObservableProperty] private string _nome = string.Empty;
        [ObservableProperty] private string _valor = string.Empty;
        [ObservableProperty] private CategoriaModel? _categoriaSelecionada;
        [ObservableProperty] private List<CategoriaModel> _categorias = new();

        public AdicionarConsumivelViewModel(
            IBaseRepository<ConsumiveisModel> repository,
            IBaseRepository<CategoriaModel> catRepository,
            ConsumiveisViewModel parent,
            ConsumiveisModel? item = null)
        {
            _repository = repository;
            _catRepository = catRepository;
            _parent = parent;
            _itemParaEdicao = item;

            if (_itemParaEdicao != null) PreencherDados();
            _ = CarregarCategorias();
        }

        private void PreencherDados()
        {
            Nome = _itemParaEdicao!.Nome;
            Valor = _itemParaEdicao.Valor.ToString("F2");
        }

        private async Task CarregarCategorias()
        {
            var cats = await _catRepository.GetAllAsync();
            Categorias = cats.ToList();

            if (_itemParaEdicao != null)
                CategoriaSelecionada = Categorias.FirstOrDefault(c => c.IdCategoria == _itemParaEdicao.IdCategoria);
        }

        // O toolkit gerará o SalvarCommand
        [RelayCommand]
        private async Task Salvar()
        {
            if (string.IsNullOrWhiteSpace(Nome) || CategoriaSelecionada == null) return;

            decimal.TryParse(Valor, out decimal preco);

            if (_itemParaEdicao == null)
            {
                var novo = new ConsumiveisModel
                {
                    Nome = Nome,
                    Valor = preco,
                    IdCategoria = CategoriaSelecionada.IdCategoria
                };
                await _repository.AddAsync(novo);
            }
            else
            {
                _itemParaEdicao.Nome = Nome;
                _itemParaEdicao.Valor = preco;
                _itemParaEdicao.IdCategoria = CategoriaSelecionada.IdCategoria;
                await _repository.UpdateAsync(_itemParaEdicao);
            }

            // Chama o método de recarregar da lista principal
            await _parent.CarregarDadosAsync();
            Voltar();
        }

        // O toolkit gerará o VoltarCommand
        [RelayCommand]
        private void Voltar()
        {
            _parent.ExibindoLista = true;
            _parent.ViewSubAtual = null;
        }
    }
}