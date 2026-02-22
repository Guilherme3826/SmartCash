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
        private readonly IBaseRepository<TransacaoModel> _transacaoRepository;
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

        public TransacoesViewModel(IBaseRepository<TransacaoModel> transacaoRepository)
        {
            _transacaoRepository = transacaoRepository;
            _ = CarregarDadosAsync();
        }

        // Construtor para Design-Time
        public TransacoesViewModel() { }

        [RelayCommand]
        public async Task CarregarDadosAsync()
        {
            // Busca as transações com Include dos Itens e Produtos para exibir o total e detalhes
            var dados = await _transacaoRepository.GetAllAsync(query =>
                query.Include(t => t.Itens)
                     .ThenInclude(i => i.Produto));

            _todasTransacoes = dados.OrderByDescending(t => t.Data).ToList();

            GerarListaDeMeses();

            // Define o filtro inicial como o mês atual ou o primeiro da lista
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
            if (string.IsNullOrEmpty(MesSelecionado))
            {
                Transacoes = new ObservableCollection<TransacaoModel>(_todasTransacoes);
                return;
            }

            var filtradas = _todasTransacoes.Where(t =>
                t.Data.ToString("MMMM yyyy", new CultureInfo("pt-BR")) == MesSelecionado).ToList();

            Transacoes = new ObservableCollection<TransacaoModel>(filtradas);
        }
        [RelayCommand]
        private void AbrirDetalhes(TransacaoModel transacao)
        {
            if (transacao == null) return;

            // Resetamos para garantir a troca de contexto
            ViewSubAtual = null;

            // Pedimos apenas a ViewModel. O XAML cuidará de criar a View.
            var vm = App.ServiceProvider.GetRequiredService<TransacaoDetalhesViewModel>();

            ViewSubAtual = vm;
            ExibindoLista = false;

            // Envia o ID para a VM que acabou de ser criada
            WeakReferenceMessenger.Default.Send(new TransacaoSelecionadaMessage(transacao.IdTransacao));
        }

        [RelayCommand]
        private void Adicionar()
        {
            ExibindoLista = false;
            // ViewSubAtual = new CadastroTransacaoView(); // Exemplo de navegação futura
        }
        partial void OnTransacaoSelecionadaChanged(TransacaoModel? value)
        {
            if (value != null)
            {
                // 1. Prepara a View de destino (ela já começa a ouvir o Messenger)
                ViewSubAtual = new TransacaoDetalhesViewModel(_transacaoRepository, this);
                ExibindoLista = false;

                // 2. Envia a mensagem com o ID
                WeakReferenceMessenger.Default.Send(new TransacaoSelecionadaMessage(value.IdTransacao));

                // 3. Limpa seleção da lista
                TransacaoSelecionada = null;
            }
        }
    }
}