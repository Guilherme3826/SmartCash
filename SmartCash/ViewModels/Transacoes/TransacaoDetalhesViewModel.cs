using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
    public class ItemDisplayModel
    {
        public ItemModel Item { get; set; } = null!;
        public DateTime Horario { get; set; }
    }

    public partial class TransacaoDetalhesViewModel : ObservableObject, IRecipient<TransacaoSelecionadaMessage>
    {
        private readonly ITransacaoRepository _transacaoRepository;
        private readonly IItemRepository _itemRepository;
        private readonly TransacoesViewModel _parentViewModel;

        [ObservableProperty]
        private TransacaoModel? _transacao;

        [ObservableProperty]
        private ObservableCollection<ItemDisplayModel> _itensDoDia = new();

        [ObservableProperty]
        private bool _estaCarregando;

        [ObservableProperty] private bool _editandoData;
        [ObservableProperty] private ItemDisplayModel? _itemSendoEditado;

        // O Avalonia DatePicker utiliza DateTimeOffset
        [ObservableProperty] private DateTimeOffset? _novaDataSelecionada;

        // Propriedade de controle interno para evitar loop infinito na troca de datas
        private bool _isProcessandoMudancaData = false;

        public TransacaoDetalhesViewModel(
            ITransacaoRepository transacaoRepository,
            IItemRepository itemRepository,
            TransacoesViewModel parentViewModel)
        {
            _transacaoRepository = transacaoRepository;
            _itemRepository = itemRepository;
            _parentViewModel = parentViewModel;

            WeakReferenceMessenger.Default.Register(this);
        }

        public async void Receive(TransacaoSelecionadaMessage message)
        {
            await CarregarDetalhesAsync(message.Value);
        }

        private async Task CarregarDetalhesAsync(int id)
        {
            EstaCarregando = true;

            var transacaoBase = await _transacaoRepository.GetByIdAsync(id);

            if (transacaoBase != null)
            {
                var todas = await _transacaoRepository.GetAllAsync();
                var transacoesDoDia = todas
                    .Where(t => t.Data.Date == transacaoBase.Data.Date)
                    .OrderByDescending(t => t.Data)
                    .ToList();

                var itensCompilados = new List<ItemDisplayModel>();
                decimal valorTotalDia = 0;

                foreach (var t in transacoesDoDia)
                {
                    valorTotalDia += t.ValorTotal;

                    if (t.Itens != null)
                    {
                        foreach (var item in t.Itens)
                        {
                            itensCompilados.Add(new ItemDisplayModel
                            {
                                Item = item,
                                Horario = t.Data
                            });
                        }
                    }
                }

                Transacao = new TransacaoModel
                {
                    Data = transacaoBase.Data.Date,
                    ValorTotal = valorTotalDia
                };

                ItensDoDia = new ObservableCollection<ItemDisplayModel>(itensCompilados);
            }

            EstaCarregando = false;
        }

        [RelayCommand]
        private void Voltar()
        {
            _parentViewModel.ExibindoLista = true;
            _parentViewModel.ViewSubAtual = null;

            WeakReferenceMessenger.Default.Unregister<TransacaoSelecionadaMessage>(this);
        }

        [RelayCommand]
        private async Task ExcluirItemAsync(ItemDisplayModel displayItem)
        {
            if (displayItem == null || displayItem.Item == null || Transacao == null) return;

            var item = displayItem.Item;
            var dataAtual = Transacao.Data.Date;

            await _itemRepository.DeleteAsync(item.IdItem);

            var transacaoMae = await _transacaoRepository.GetByIdAsync(item.IdTransacao);
            if (transacaoMae != null)
            {
                if (transacaoMae.Itens.Count <= 0)
                {
                    await _transacaoRepository.DeleteAsync(transacaoMae.IdTransacao);
                }
                else
                {
                    transacaoMae.ValorTotal -= item.ValorTotal;
                    await _transacaoRepository.UpdateAsync(transacaoMae);
                }
            }

            var todas = await _transacaoRepository.GetAllAsync();
            var restanteNoDia = todas.FirstOrDefault(t => t.Data.Date == dataAtual);

            WeakReferenceMessenger.Default.Send(new NovaTransacaoAdicionada());

            if (restanteNoDia != null)
            {
                await CarregarDetalhesAsync(restanteNoDia.IdTransacao);
            }
            else
            {
                Voltar();
            }
        }

        [RelayCommand]
        private void AbrirEdicaoData(ItemDisplayModel displayItem)
        {
            if (displayItem == null || displayItem.Item == null) return;

            // Bloqueia temporariamente o disparo do evento de mudança enquanto preenchemos a data inicial
            _isProcessandoMudancaData = true;
            ItemSendoEditado = displayItem;
            NovaDataSelecionada = new DateTimeOffset(displayItem.Horario);
            _isProcessandoMudancaData = false;

            EditandoData = true;
        }

        [RelayCommand]
        private void CancelarEdicao()
        {
            EditandoData = false;
            ItemSendoEditado = null;
        }

        // Este método é acionado automaticamente pelo CommunityToolkit toda vez que a propriedade NovaDataSelecionada mudar
        partial void OnNovaDataSelecionadaChanged(DateTimeOffset? value)
        {
            if (!_isProcessandoMudancaData && value.HasValue && EditandoData)
            {
                // Se o usuário mudou a data no DatePicker e não estamos num bloqueio de sistema, salva automaticamente.
                _ = SalvarNovaDataAsync();
            }
        }

        private async Task SalvarNovaDataAsync()
        {
            if (ItemSendoEditado == null || ItemSendoEditado.Item == null || NovaDataSelecionada == null || Transacao == null) return;

            // Bloqueia chamadas duplas
            _isProcessandoMudancaData = true;

            var item = ItemSendoEditado.Item;
            var dataAtualDaTransacao = Transacao.Data.Date;
            var novaData = NovaDataSelecionada.Value.Date;

            if (dataAtualDaTransacao == novaData)
            {
                CancelarEdicao();
                _isProcessandoMudancaData = false;
                return;
            }

            var todasTransacoes = await _transacaoRepository.GetAllAsync();
            var transacaoAntiga = await _transacaoRepository.GetByIdAsync(item.IdTransacao);

            if (transacaoAntiga == null)
            {
                _isProcessandoMudancaData = false;
                return;
            }

            var transacaoDestino = todasTransacoes.FirstOrDefault(t => t.Data.Date == novaData);

            if (transacaoDestino != null)
            {
                transacaoDestino.ValorTotal += item.ValorTotal;
                await _transacaoRepository.UpdateAsync(transacaoDestino);

                item.IdTransacao = transacaoDestino.IdTransacao;
                await _itemRepository.UpdateAsync(item);
            }
            else
            {
                var novaTransacao = new TransacaoModel
                {
                    Data = novaData.Add(DateTime.Now.TimeOfDay),
                    ValorTotal = item.ValorTotal
                };
                await _transacaoRepository.AddAsync(novaTransacao);

                item.IdTransacao = novaTransacao.IdTransacao;
                await _itemRepository.UpdateAsync(item);
            }

            if (transacaoAntiga.Itens.Count <= 1)
            {
                await _transacaoRepository.DeleteAsync(transacaoAntiga.IdTransacao);
            }
            else
            {
                transacaoAntiga.ValorTotal -= item.ValorTotal;
                await _transacaoRepository.UpdateAsync(transacaoAntiga);
            }

            EditandoData = false;
            ItemSendoEditado = null;

            // Aciona o recarregamento da tela principal e gráficos
            WeakReferenceMessenger.Default.Send(new NovaTransacaoAdicionada());

            var transacoesRestantesHoje = await _transacaoRepository.GetAllAsync();
            var restanteNoDia = transacoesRestantesHoje.FirstOrDefault(t => t.Data.Date == dataAtualDaTransacao);

            if (restanteNoDia != null)
            {
                await CarregarDetalhesAsync(restanteNoDia.IdTransacao);
            }
            else
            {
                Voltar();
            }

            _isProcessandoMudancaData = false;
        }
    }
}