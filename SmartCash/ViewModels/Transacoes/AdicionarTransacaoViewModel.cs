using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Transacoes
{
    public partial class AdicionarTransacaoViewModel : ObservableObject
    {
        private readonly IBaseRepository<ConsumiveisModel> _produtoRepository;
        private readonly ITransacaoRepository _transacaoRepository;
        private readonly TransacoesViewModel _parentViewModel;

        [ObservableProperty] private List<ConsumiveisModel> _produtosSugestao = new();
        [ObservableProperty] private ConsumiveisModel? _produtoSelecionado;
        [ObservableProperty] private string _buscaTexto = string.Empty;
        [ObservableProperty] private decimal _valorTotalTemp;

        // --- NOVAS PROPRIEDADES ---
        [ObservableProperty] private string _quantidadeInput = "1";
        [ObservableProperty] private string _precoUnitarioInput = string.Empty;

        public ObservableCollection<ItemModel> ItensTemporarios { get; } = new();

        public AdicionarTransacaoViewModel(
            IBaseRepository<ConsumiveisModel> produtoRepository,
            ITransacaoRepository transacaoRepository,
            TransacoesViewModel parent)
        {
            _produtoRepository = produtoRepository;
            _transacaoRepository = transacaoRepository;
            _parentViewModel = parent;
            _ = CarregarProdutosSugestaoAsync();
        }

        // Monitora quando um produto é selecionado na AutoComplete para sugerir o preço atual
        partial void OnProdutoSelecionadoChanged(ConsumiveisModel? value)
        {
            if (value != null)
            {
                PrecoUnitarioInput = value.Valor.ToString("F2");
            }
        }

        [RelayCommand]
        private void IncrementarQuantidade()
        {
            if (int.TryParse(QuantidadeInput, out int atual))
            {
                QuantidadeInput = (atual + 1).ToString();
            }
            else
            {
                QuantidadeInput = "1";
            }
        }

        [RelayCommand]
        private void AdicionarItem()
        {
            // 1. Identifica o produto (pela seleção ou pelo texto digitado)
            var produto = ProdutoSelecionado ?? ProdutosSugestao.FirstOrDefault(p =>
                p.Nome.Equals(BuscaTexto, StringComparison.OrdinalIgnoreCase));

            if (produto == null) return;

            // 2. Parse dos inputs com segurança
            if (!int.TryParse(QuantidadeInput, out int qtd) || qtd <= 0)
                qtd = 1;

            // Garante que o parse decimal use a cultura correta (ponto ou vírgula)
            if (!decimal.TryParse(PrecoUnitarioInput, System.Globalization.NumberStyles.Currency, null, out decimal precoUnit) || precoUnit <= 0)
                precoUnit = produto.Valor;

            // 3. Cria o novo item
            var novoItem = new ItemModel
            {
                IdConsumivel = produto.IdConsumivel,
                Produto = produto,
                Quantidade = qtd,
                ValorUnit = precoUnit,
                ValorTotal = qtd * precoUnit
            };

            // 4. Adiciona à lista temporária
            ItensTemporarios.Add(novoItem);

            // 5. Atualiza os cálculos de totais da tela ANTES de limpar a entrada
            AtualizarCalculos();

            // 6. Limpa os campos de entrada (esta chamada dispara as notificações de PropertyChanged)
            LimparCamposEntrada();
        }

        private void LimparCamposEntrada()
        {
            // Primeiro limpamos o texto da busca para "matar" o filtro
            BuscaTexto = string.Empty;
            OnPropertyChanged(nameof(BuscaTexto));

            // Depois limpamos o objeto selecionado
            ProdutoSelecionado = null;
            OnPropertyChanged(nameof(ProdutoSelecionado));

            // Resetamos os demais campos
            QuantidadeInput = "1";
            PrecoUnitarioInput = string.Empty;
        }

        private void AtualizarCalculos() => ValorTotalTemp = ItensTemporarios.Sum(i => i.ValorTotal);

        [RelayCommand]
        private void RemoverItem(ItemModel item)
        {
            if (item != null)
            {
                ItensTemporarios.Remove(item);
                AtualizarCalculos();
            }
        }

        [RelayCommand]
        private async Task SalvarTransacao()
        {
            if (!ItensTemporarios.Any()) return;

            var novaTransacao = new TransacaoModel
            {
                Data = DateTime.Now,
                ValorTotal = ValorTotalTemp,
                Itens = ItensTemporarios.Select(i => new ItemModel
                {
                    IdConsumivel = i.IdConsumivel,
                    Quantidade = i.IdConsumivel == 0 ? 0 : i.Quantidade, // Ajuste conforme sua regra de negócio
                    ValorUnit = i.ValorUnit,
                    ValorTotal = i.ValorTotal
                }).ToList()
            };

            await _transacaoRepository.AddAsync(novaTransacao);
            await _parentViewModel.CarregarDadosCommand.ExecuteAsync(null);
            Voltar();

            WeakReferenceMessenger.Default.Send(new NovaTransacaoAdicionada{});
        }

        [RelayCommand]
        private void Voltar()
        {
            _parentViewModel.ExibindoLista = true;
            _parentViewModel.ViewSubAtual = null;
        }

        private async Task CarregarProdutosSugestaoAsync()
        {
            var produtos = await _produtoRepository.GetAllAsync();
            ProdutosSugestao = produtos.ToList();
        }
    }
}