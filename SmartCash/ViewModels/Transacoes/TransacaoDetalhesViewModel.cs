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
    // Classe auxiliar para mesclar o item com o horário da sua respectiva transação
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

        // Nova propriedade declarada para ancorar a lista de itens com horário no XAML
        [ObservableProperty]
        private ObservableCollection<ItemDisplayModel> _itensDoDia = new();

        [ObservableProperty]
        private bool _estaCarregando;

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

            // 1. Busca a transação específica para descobrir qual foi o dia selecionado
            var transacaoBase = await _transacaoRepository.GetByIdAsync(id);

            if (transacaoBase != null)
            {
                // 2. Busca todas as transações para poder filtrar as do mesmo dia
                var todas = await _transacaoRepository.GetAllAsync();
                var transacoesDoDia = todas
                    .Where(t => t.Data.Date == transacaoBase.Data.Date)
                    .OrderByDescending(t => t.Data) // Ordena das mais recentes para as mais antigas no dia
                    .ToList();

                var itensCompilados = new List<ItemDisplayModel>();
                decimal valorTotalDia = 0;

                // 3. Varre todas as transações do dia para somar o valor e aglutinar os itens
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
                                Horario = t.Data // Salva o horário exato em que esta transação ocorreu
                            });
                        }
                    }
                }

                // 4. Cria um objeto Transacao virtual apenas para alimentar o cabeçalho com a soma total do dia
                Transacao = new TransacaoModel
                {
                    Data = transacaoBase.Data.Date,
                    ValorTotal = valorTotalDia
                };

                // 5. Alimenta a lista que será exibida no XAML
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

            // 1. Exclui o item no banco de dados
            await _itemRepository.DeleteAsync(item.IdItem);

            // 2. Verifica o status da transação mãe daquele item específico
            var transacaoMae = await _transacaoRepository.GetByIdAsync(item.IdTransacao);
            if (transacaoMae != null)
            {
                // Se a transação mãe ficou vazia após excluir o item, nós a apagamos
                if (transacaoMae.Itens.Count == 0)
                {
                    await _transacaoRepository.DeleteAsync(transacaoMae.IdTransacao);
                }
                else
                {
                    // Se ainda há itens nela, atualizamos o valor descontando o item excluído
                    transacaoMae.ValorTotal -= item.ValorTotal;
                    await _transacaoRepository.UpdateAsync(transacaoMae);
                }
            }

            // 3. Verifica se sobraram outras transações neste mesmo dia
            var todas = await _transacaoRepository.GetAllAsync();
            var restanteNoDia = todas.FirstOrDefault(t => t.Data.Date == dataAtual);

            // 4. Notifica o sistema global (Dashboard e Lista mãe) para atualizar os cálculos
            WeakReferenceMessenger.Default.Send(new NovaTransacaoAdicionada());

            if (restanteNoDia != null)
            {
                // Recarrega os dados do dia utilizando um ID válido remanescente
                await CarregarDetalhesAsync(restanteNoDia.IdTransacao);
            }
            else
            {
                // Se não sobrou nenhum item no dia todo, fechamos os detalhes
                Voltar();
            }
        }
    }
}