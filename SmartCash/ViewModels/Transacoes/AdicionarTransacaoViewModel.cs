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
using System.Globalization; // Adicionado para garantir o parse correto
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

            WeakReferenceMessenger.Default.Register<NovoConsumivelAdicionado>(this, (r, m) =>
            {
                _ = CarregarProdutosSugestaoAsync();
            });
        }

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
            // Alterado para decimal para suportar incremento mesmo se houver valor quebrado (ex: 1.5 + 1 = 2.5)
            if (decimal.TryParse(QuantidadeInput, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal atual))
            {
                QuantidadeInput = (atual + 1).ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                QuantidadeInput = "1";
            }
        }

        [RelayCommand]
        private void AdicionarItem()
        {
            var produto = ProdutoSelecionado ?? ProdutosSugestao.FirstOrDefault(p =>
                p.Nome.Equals(BuscaTexto, StringComparison.OrdinalIgnoreCase));

            if (produto == null) return;

            // CORREÇÃO: Usando decimal.TryParse para aceitar pesos como 0,5kg ou 1,250kg
            if (!decimal.TryParse(QuantidadeInput, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal qtd) || qtd <= 0)
                qtd = 1;

            if (!decimal.TryParse(PrecoUnitarioInput, NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal precoUnit) || precoUnit <= 0)
                precoUnit = produto.Valor;

            var novoItem = new ItemModel
            {
                IdConsumivel = produto.IdConsumivel,
                Produto = produto,
                Quantidade = (Decimal)qtd, // Cast para double se sua Model ainda for double, ou mantenha decimal se já alterou a Model
                ValorUnit = precoUnit,
                ValorTotal = qtd * precoUnit
            };

            ItensTemporarios.Add(novoItem);
            AtualizarCalculos();
            LimparCamposEntrada();
        }

        private void LimparCamposEntrada()
        {
            BuscaTexto = string.Empty;
            OnPropertyChanged(nameof(BuscaTexto));

            ProdutoSelecionado = null;
            OnPropertyChanged(nameof(ProdutoSelecionado));

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
                    Quantidade = i.IdConsumivel == 0 ? 0 : i.Quantidade,
                    ValorUnit = i.ValorUnit,
                    ValorTotal = i.ValorTotal
                }).ToList()
            };

            await _transacaoRepository.AddAsync(novaTransacao);
            await _parentViewModel.CarregarDadosCommand.ExecuteAsync(null);
            Voltar();

            WeakReferenceMessenger.Default.Send(new NovaTransacaoAdicionada { });
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