using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Transacoes
{
    public partial class TransacaoDetalhesViewModel : ObservableObject, IRecipient<TransacaoSelecionadaMessage>
    {
        private readonly ITransacaoRepository _transacaoRepository;
        private readonly TransacoesViewModel _parentViewModel;

        [ObservableProperty]
        private TransacaoModel? _transacao;

        [ObservableProperty]
        private bool _estaCarregando;

        public TransacaoDetalhesViewModel(ITransacaoRepository transacaoRepository, TransacoesViewModel parentViewModel)
        {
            _transacaoRepository = transacaoRepository;
            _parentViewModel = parentViewModel;

            // Registra-se para ouvir a mensagem de ID selecionado
            WeakReferenceMessenger.Default.Register(this);
        }

        public async void Receive(TransacaoSelecionadaMessage message)
        {
            await CarregarDetalhesAsync(message.Value);
        }

        private async Task CarregarDetalhesAsync(int id)
        {
            EstaCarregando = true;

            // Aqui utilizamos a query já declarada no seu repositório
            // que deve conter os Includes necessários para Itens, Produto e Categoria
            var dados = await _transacaoRepository.GetByIdAsync(id);

            Transacao = dados;
            EstaCarregando = false;
        }

        [RelayCommand]
        private void Voltar()
        {
            _parentViewModel.ExibindoLista = true;
            _parentViewModel.ViewSubAtual = null;

            // É boa prática cancelar o registro ao sair da view
            WeakReferenceMessenger.Default.Unregister<TransacaoSelecionadaMessage>(this);
        }
    }
}