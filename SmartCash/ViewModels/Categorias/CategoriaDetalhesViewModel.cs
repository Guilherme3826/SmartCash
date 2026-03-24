using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Categorias
{
    // Classe auxiliar para mesclar o item com a data de sua respectiva transação
    public class CategoriaItemDisplayModel
    {
        public ItemModel Item { get; set; } = null!;
        public DateTime DataCompra { get; set; }
    }

    public partial class CategoriaDetalhesViewModel : ObservableObject, IRecipient<CategoriaDetalhesMessage>
    {
        private readonly IBaseRepository<CategoriaModel> _categoriaRepository;
        private readonly CategoriasViewModel _parentViewModel;

        [ObservableProperty]
        private CategoriaModel? _categoria;

        [ObservableProperty]
        private string _periodoFiltroAtual = string.Empty;

        [ObservableProperty]
        private decimal _totalGastoNoPeriodo;

        [ObservableProperty]
        private ObservableCollection<CategoriaItemDisplayModel> _itens = new();

        [ObservableProperty]
        private bool _estaCarregando;

        public CategoriaDetalhesViewModel(IBaseRepository<CategoriaModel> categoriaRepository, CategoriasViewModel parentViewModel)
        {
            _categoriaRepository = categoriaRepository;
            _parentViewModel = parentViewModel;

            WeakReferenceMessenger.Default.Register(this);
        }

        public async void Receive(CategoriaDetalhesMessage message)
        {
            PeriodoFiltroAtual = message.PeriodoFiltro;
            await CarregarDetalhesAsync(message.IdCategoria);
        }

        private async Task CarregarDetalhesAsync(int id)
        {
            EstaCarregando = true;

            var cat = await _categoriaRepository.GetByIdAsync(id);

            if (cat != null)
            {
                Categoria = cat;

                // Achata a lista de itens e remove os que não estão vinculados a transações
                var itensValidos = cat.Produtos
                    .SelectMany(p => p.Itens)
                    .Where(i => i.Transacao != null);

                // Aplica a mesma regra de filtro da tela pai
                if (PeriodoFiltroAtual != "Todos os Períodos" && !string.IsNullOrEmpty(PeriodoFiltroAtual))
                {
                    if (PeriodoFiltroAtual.StartsWith("Ano "))
                    {
                        var anoFiltro = PeriodoFiltroAtual.Replace("Ano ", "");
                        itensValidos = itensValidos.Where(i => i.Transacao.Data.Year.ToString() == anoFiltro);
                    }
                    else
                    {
                        itensValidos = itensValidos.Where(i => i.Transacao.Data.ToString("MMMM yyyy", new CultureInfo("pt-BR")) == PeriodoFiltroAtual);
                    }
                }

                TotalGastoNoPeriodo = itensValidos.Sum(i => i.ValorTotal);

                // Prepara a lista ordenando da compra mais recente para a mais antiga
                var itensParaExibicao = itensValidos
                    .OrderByDescending(i => i.Transacao.Data)
                    .Select(i => new CategoriaItemDisplayModel
                    {
                        Item = i,
                        DataCompra = i.Transacao.Data
                    })
                    .ToList();

                Itens = new ObservableCollection<CategoriaItemDisplayModel>(itensParaExibicao);
            }

            EstaCarregando = false;
        }

        [RelayCommand]
        private void Voltar()
        {
            _parentViewModel.ExibindoLista = true;
            _parentViewModel.ViewSubAtual = null;

            WeakReferenceMessenger.Default.Unregister<CategoriaDetalhesMessage>(this);
        }
    }
}