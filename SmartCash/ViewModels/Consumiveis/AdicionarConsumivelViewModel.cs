using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Consumiveis
{
    public partial class AdicionarConsumivelViewModel : ObservableObject
    {
        private readonly IBaseRepository<ConsumiveisModel> _repository;
        private readonly IBaseRepository<CategoriaModel> _catRepository;

        private ConsumiveisViewModel? _parent;
        private ConsumiveisModel? _itemParaEdicao;

        [ObservableProperty] private string _nome = string.Empty;
        [ObservableProperty] private string _valor = string.Empty;
        [ObservableProperty] private CategoriaModel? _categoriaSelecionada;
        [ObservableProperty] private List<CategoriaModel> _categorias = new();

        // Propriedades para controle do aviso na interface
        [ObservableProperty] private bool _mostrarAviso;
        [ObservableProperty] private string _mensagemAviso = string.Empty;

        public AdicionarConsumivelViewModel(
            IBaseRepository<ConsumiveisModel> repository,
            IBaseRepository<CategoriaModel> catRepository)
        {
            _repository = repository;
            _catRepository = catRepository;
        }

        // Método chamado logo após a injeção para configurar se é uma edição ou adição
        public void Inicializar(ConsumiveisViewModel parent, ConsumiveisModel? item = null)
        {
            _parent = parent;
            _itemParaEdicao = item;
            MostrarAviso = false;

            if (_itemParaEdicao != null)
            {
                PreencherDados();
            }
            else
            {
                Nome = string.Empty;
                Valor = string.Empty;
                CategoriaSelecionada = null;
            }

            _ = CarregarCategorias();
        }

        private void PreencherDados()
        {
            if (_itemParaEdicao != null)
            {
                Nome = _itemParaEdicao.Nome;
                Valor = _itemParaEdicao.Valor.ToString("F2");
            }
        }

        private async Task CarregarCategorias()
        {
            var cats = await _catRepository.GetAllAsync();
            Categorias = cats.ToList();

            if (_itemParaEdicao != null)
                CategoriaSelecionada = Categorias.FirstOrDefault(c => c.IdCategoria == _itemParaEdicao.IdCategoria);
        }

        [RelayCommand]
        private async Task Salvar()
        {
            if (string.IsNullOrWhiteSpace(Nome) || CategoriaSelecionada == null) return;

            // Validação de Duplicidade
            var todosConsumiveis = await _repository.GetAllAsync();
            bool nomeDuplicado = todosConsumiveis.Any(c =>
                c.Nome.Equals(Nome.Trim(), StringComparison.OrdinalIgnoreCase) &&
                c.IdConsumivel != (_itemParaEdicao?.IdConsumivel ?? 0));

            if (nomeDuplicado)
            {
                MensagemAviso = "Já existe um produto ou serviço cadastrado com este nome.";
                MostrarAviso = true;
                return;
            }

            MostrarAviso = false;
            decimal.TryParse(Valor, out decimal preco);

            if (_itemParaEdicao == null)
            {
                var novo = new ConsumiveisModel
                {
                    Nome = Nome.Trim(),
                    Valor = preco,
                    IdCategoria = CategoriaSelecionada.IdCategoria
                };
                await _repository.AddAsync(novo);
            }
            else
            {
                _itemParaEdicao.Nome = Nome.Trim();
                _itemParaEdicao.Valor = preco;
                _itemParaEdicao.IdCategoria = CategoriaSelecionada.IdCategoria;
                await _repository.UpdateAsync(_itemParaEdicao);
            }

            if (_parent != null)
            {
                await _parent.CarregarDadosAsync();
            }

            WeakReferenceMessenger.Default.Send(new NovoConsumivelAdicionado { });

            Voltar();
        }

        [RelayCommand]
        private void Voltar()
        {
            if (_parent != null)
            {
                _parent.ExibindoLista = true;
                _parent.ViewSubAtual = null;
            }
        }
    }
}