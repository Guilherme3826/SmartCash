using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using SmartCash.Views.Categorias;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Categorias
{
    // Classe auxiliar para exibir a categoria junto com o seu total de gastos no período
    public class CategoriaDisplayModel
    {
        public CategoriaModel Categoria { get; set; } = null!;
        public decimal TotalGasto { get; set; }
    }

    public partial class CategoriasViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? _viewSubAtual;

        [ObservableProperty]
        private bool _exibindoLista = true;

        [ObservableProperty]
        private ObservableCollection<string> _opcoesFiltro = new();

        [ObservableProperty]
        private string? _filtroSelecionado;

        private readonly IBaseRepository<CategoriaModel> _categoriaRepository;
        private List<CategoriaModel> _todasCategoriasBase = new();

        // Alterado o tipo genérico para suportar o wrapper com o TotalGasto
        [ObservableProperty]
        private ObservableCollection<CategoriaDisplayModel> _categorias = new();

        // O construtor agora recebe a interface vinculada ao Modelo correto
        public CategoriasViewModel(IBaseRepository<CategoriaModel> categoriaRepository)
        {
  
            ExibindoLista = true;
            _categoriaRepository = categoriaRepository;
            _ = CarregarCategorias();
        }
        // Adicione este método na sua CategoriasViewModel.cs
        public void AbrirDetalhesExternamente(int idCategoria, string periodoFiltro)
        {
            // 1. Prepara a ViewModel de detalhes
            ViewSubAtual = null;
            var vm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<CategoriaDetalhesViewModel>(App.ServiceProvider);
            ViewSubAtual = vm;
            ExibindoLista = false;

            // 2. Envia os parâmetros do Dashboard para carregar os itens corretos
            WeakReferenceMessenger.Default.Send(new CategoriaDetalhesMessage(idCategoria, periodoFiltro));
        }

        public CategoriasViewModel()
        {
        }

        [RelayCommand]
        private void AdicionarNovaCategoria()
        {
            ViewSubAtual = null;

            var vm = App.ServiceProvider.GetRequiredService<AdicionarCategoriaViewModel>();
            ViewSubAtual = vm;
            ExibindoLista = false;
        }

        public async Task CarregarCategorias()
        {
            var lista = await _categoriaRepository.GetAllAsync();
            _todasCategoriasBase = lista.ToList();

            GerarOpcoesDeFiltro();

            FiltroSelecionado = OpcoesFiltro.FirstOrDefault();
            AplicarFiltro();
        }

        private void GerarOpcoesDeFiltro()
        {
            // Extrai as datas de todas as transações que possuam itens associados a estas categorias
            var datas = _todasCategoriasBase
                .SelectMany(c => c.Produtos)
                .SelectMany(p => p.Itens)
                .Where(i => i.Transacao != null)
                .Select(i => i.Transacao.Data)
                .ToList();

            var opcoes = new List<string> { "Todos os Períodos" };

            // Agrupa os Anos
            var anos = datas.Select(d => $"Ano {d.Year}").Distinct().OrderByDescending(a => a);
            opcoes.AddRange(anos);

            // Agrupa os Meses exatos
            var mesesOrdenados = datas.Select(d => new DateTime(d.Year, d.Month, 1))
                                      .Distinct()
                                      .OrderByDescending(d => d)
                                      .Select(d => d.ToString("MMMM yyyy", new CultureInfo("pt-BR")));
            opcoes.AddRange(mesesOrdenados);

            // Caso o banco esteja vazio, assegura a exibição do mês atual
            if (!opcoes.Any(o => o.Contains(DateTime.Now.ToString("yyyy"))))
            {
                opcoes.Insert(1, $"Ano {DateTime.Now.Year}");
                opcoes.Insert(2, DateTime.Now.ToString("MMMM yyyy", new CultureInfo("pt-BR")));
            }

            OpcoesFiltro = new ObservableCollection<string>(opcoes);
        }

        partial void OnFiltroSelecionadoChanged(string? value)
        {
            AplicarFiltro();
        }

        private void AplicarFiltro()
        {
            var listaExibicao = new List<CategoriaDisplayModel>();

            foreach (var cat in _todasCategoriasBase)
            {
                var itensValidos = cat.Produtos
                    .SelectMany(p => p.Itens)
                    .Where(i => i.Transacao != null);

                if (FiltroSelecionado != "Todos os Períodos" && !string.IsNullOrEmpty(FiltroSelecionado))
                {
                    if (FiltroSelecionado.StartsWith("Ano "))
                    {
                        var anoFiltro = FiltroSelecionado.Replace("Ano ", "");
                        itensValidos = itensValidos.Where(i => i.Transacao.Data.Year.ToString() == anoFiltro);
                    }
                    else
                    {
                        itensValidos = itensValidos.Where(i => i.Transacao.Data.ToString("MMMM yyyy", new CultureInfo("pt-BR")) == FiltroSelecionado);
                    }
                }

                listaExibicao.Add(new CategoriaDisplayModel
                {
                    Categoria = cat,
                    TotalGasto = itensValidos.Sum(i => i.ValorTotal)
                });
            }

            // Ordena as categorias pelo maior gasto e, depois, pelo nome
            var listaOrdenada = listaExibicao
                .OrderByDescending(c => c.TotalGasto)
                .ThenBy(c => c.Categoria.Nome)
                .ToList();

            Categorias = new ObservableCollection<CategoriaDisplayModel>(listaOrdenada);
        }

        [RelayCommand]
        private void AbrirDetalhes(CategoriaDisplayModel categoriaDisplay)
        {
            if (categoriaDisplay == null) return;

            ViewSubAtual = null;

            var vm = App.ServiceProvider.GetRequiredService<CategoriaDetalhesViewModel>();
            ViewSubAtual = vm;
            ExibindoLista = false;

            // Envia o ID da categoria e o filtro que estava selecionado no ComboBox
            string filtroAtual = FiltroSelecionado ?? "Todos os Períodos";
            WeakReferenceMessenger.Default.Send(new CategoriaDetalhesMessage(categoriaDisplay.Categoria.IdCategoria, filtroAtual));
        }

        [RelayCommand]
        private async Task ExcluirCategoriaAsync(CategoriaDisplayModel categoriaDisplay)
        {
            if (categoriaDisplay == null || categoriaDisplay.Categoria == null) return;

            try
            {
                await _categoriaRepository.DeleteAsync(categoriaDisplay.Categoria.IdCategoria);

                // Recarrega do banco para atualizar todos os cálculos do filtro
                await CarregarCategorias();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERRO AO EXCLUIR] {ex.Message}");
            }
        }
    }
}