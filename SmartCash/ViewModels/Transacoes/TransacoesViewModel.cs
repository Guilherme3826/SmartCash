using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using SmartCash.Views.Transacoes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Transacoes
{
    public partial class TransacoesViewModel : ObservableObject
    {
        private readonly ITransacaoRepository _transacaoRepository;
        private List<TransacaoModel> _todasTransacoes = new List<TransacaoModel>();

        [ObservableProperty]
        private ObservableCollection<TransacaoModel> _transacoes = new ObservableCollection<TransacaoModel>();

        [ObservableProperty]
        private TransacaoModel? _transacaoSelecionada;

        [ObservableProperty]
        private ObservableCollection<string> _mesesFiltro = new ObservableCollection<string>();

        [ObservableProperty]
        private string? _mesSelecionado;

        [ObservableProperty]
        private bool _exibindoLista = true;

        [ObservableProperty]
        private object? _viewSubAtual;

        public TransacoesViewModel(ITransacaoRepository transacaoRepository)
        {
            _transacaoRepository = transacaoRepository;
            _ = CarregarDadosAsync();
        }

        // Construtor para Design-Time
        public TransacoesViewModel() { }

        [RelayCommand]
        public async Task CarregarDadosAsync()
        {
            var dados = await _transacaoRepository.GetAllAsync();

            _todasTransacoes = dados.OrderByDescending(t => t.Data).ToList();

            GerarListaDeMeses();

            MesSelecionado = MesesFiltro.FirstOrDefault();
            AplicarFiltro();
        }

        private void GerarListaDeMeses()
        {
            var meses = _todasTransacoes
                .Select(t => t.Data.ToString("MMMM yyyy", new CultureInfo("pt-BR")))
                .Distinct()
                .ToList();

            if (!meses.Any())
            {
                meses.Add(DateTime.Now.ToString("MMMM yyyy", new CultureInfo("pt-BR")));
            }

            MesesFiltro = new ObservableCollection<string>(meses);
        }

        partial void OnMesSelecionadoChanged(string? value)
        {
            AplicarFiltro();
        }

        private void AplicarFiltro()
        {
            IEnumerable<TransacaoModel> filtradas = _todasTransacoes;

            if (!string.IsNullOrEmpty(MesSelecionado))
            {
                filtradas = _todasTransacoes.Where(t =>
                    t.Data.ToString("MMMM yyyy", new CultureInfo("pt-BR")) == MesSelecionado);
            }

            // Agrupa as transações pelo dia e cria um único objeto virtual contendo a soma dos valores e a união dos itens
            var resumoDiario = filtradas
                .GroupBy(t => t.Data.Date)
                .Select(g => new TransacaoModel
                {
                    IdTransacao = g.First().IdTransacao, // Mantém a referência primária
                    Data = g.Key,
                    ValorTotal = g.Sum(x => x.ValorTotal),
                    Itens = g.SelectMany(x => x.Itens).ToList()
                })
                .OrderByDescending(t => t.Data)
                .ToList();

            Transacoes = new ObservableCollection<TransacaoModel>(resumoDiario);
        }

        [RelayCommand]
        private void AbrirDetalhes(TransacaoModel transacao)
        {
            if (transacao == null) return;

            ViewSubAtual = null;

            var vm = App.ServiceProvider.GetRequiredService<TransacaoDetalhesViewModel>();

            ViewSubAtual = vm;
            ExibindoLista = false;

            WeakReferenceMessenger.Default.Send(new TransacaoSelecionadaMessage(transacao.IdTransacao));
        }

        [RelayCommand]
        private void Adicionar()
        {
            var vm = App.ServiceProvider.GetRequiredService<AdicionarTransacaoViewModel>();
            ViewSubAtual = vm;
            ExibindoLista = false;
        }

        [RelayCommand]
        private async Task ExcluirTransacao(TransacaoModel transacao)
        {
            if (transacao == null) return;

            try
            {
                // Como aglutinamos as transações do dia na visualização, precisamos deletar todas as do banco referentes ao dia
                var transacoesDoDia = _todasTransacoes.Where(t => t.Data.Date == transacao.Data.Date).ToList();

                foreach (var t in transacoesDoDia)
                {
                    await _transacaoRepository.DeleteAsync(t.IdTransacao);
                }

                await CarregarDadosAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao excluir transação: {ex.Message}");
            }
        }

        partial void OnTransacaoSelecionadaChanged(TransacaoModel? value)
        {
            if (value != null)
            {
                ViewSubAtual = new TransacaoDetalhesViewModel(_transacaoRepository, this);
                ExibindoLista = false;

                WeakReferenceMessenger.Default.Send(new TransacaoSelecionadaMessage(value.IdTransacao));

                TransacaoSelecionada = null;
            }
        }
    }
}