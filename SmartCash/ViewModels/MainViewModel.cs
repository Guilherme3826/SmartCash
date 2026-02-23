using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SmartCash.EfCore.Interfaces;

using SmartCash.EfCore.Models;
using SmartCash.Views.Categorias;
using SmartCash.Views.Consumiveis;
using SmartCash.Views.Transacoes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartCash.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly ITransacaoRepository _transacaoRepository;
        private readonly IItemRepository _itemRepository;

        [ObservableProperty]
        private object? _viewAtual;

        [ObservableProperty]
        private bool _exibindoMenuPrincipal = true;

        [ObservableProperty]
        private bool _isPaneOpen;

        [ObservableProperty]
        private string _totalMesAtual = "R$ 0,00";

        [ObservableProperty]
        private ObservableCollection<ResumoMesModel> _historicoMensal = new();

        [ObservableProperty]
        private ObservableCollection<ItemModel> _itensMesSelecionado = new();

        [ObservableProperty]
        private ResumoMesModel? _mesSelecionado;

        [ObservableProperty]
        private ObservableCollection<ResumoCategoriaModel> _categoriasMesSelecionado = new();

        [ObservableProperty]
        private IEnumerable<ISeries> _seriesGraficoPizza = Enumerable.Empty<ISeries>();

        [ObservableProperty]
        private double _valorMaximoGauge = 100;

        // Propriedades para o Gauge
        [ObservableProperty]
        private double _vanesa;

        [ObservableProperty]
        private double _charles;

        [ObservableProperty]
        private double _ana;

        public Func<ChartPoint, string> LabelFormatter { get; set; }

        public MainViewModel(ITransacaoRepository transacaoRepository, IItemRepository itemRepository)
        {
            _transacaoRepository = transacaoRepository;
            _itemRepository = itemRepository;
            ExibindoMenuPrincipal = true;

            LabelFormatter = point => $"{point.Context.Series.Name}";

            _ = CarregarDashboardAsync();
        }

        partial void OnMesSelecionadoChanged(ResumoMesModel? value)
        {
            if (value != null)
            {
                _ = CarregarItensDoMesSelecionadoAsync(value);
            }
        }

        public async Task CarregarDashboardAsync()
        {
            int? mesIdAnterior = MesSelecionado?.Mes;
            int? anoAnterior = MesSelecionado?.Ano;

            var historico = await _transacaoRepository.GetHistoricoMensalAsync();
            HistoricoMensal.Clear();

            if (historico.Any())
            {
                decimal maxTotal = historico.Max(x => x.Total);
                foreach (var mes in historico)
                {
                    mes.AlturaBarra = maxTotal > 0 ? (double)(mes.Total / maxTotal) * 100 : 5;
                    HistoricoMensal.Add(mes);
                }

                if (mesIdAnterior.HasValue && anoAnterior.HasValue)
                {
                    var correspondente = HistoricoMensal.FirstOrDefault(x => x.Mes == mesIdAnterior && x.Ano == anoAnterior);
                    if (correspondente != null)
                    {
                        MesSelecionado = correspondente;
                        return;
                    }
                }

                var dataAtual = DateTime.Now;
                MesSelecionado = HistoricoMensal.FirstOrDefault(x => x.Mes == dataAtual.Month && x.Ano == dataAtual.Year) ?? HistoricoMensal.First();
            }
        }

        private async Task CarregarItensDoMesSelecionadoAsync(ResumoMesModel mes)
        {
            var itens = await _itemRepository.GetItensPorMesAnoAsync(mes.Mes, mes.Ano);

            ItensMesSelecionado.Clear();
            foreach (var item in itens)
            {
                ItensMesSelecionado.Add(item);
            }

            CategoriasMesSelecionado.Clear();
            var resumo = itens
                .GroupBy(i => i.Produto.IdCategoria)
                .Select(g => new ResumoCategoriaModel
                {
                    Categoria = g.First().Produto.Categoria,
                    Total = g.Sum(i => i.ValorTotal)
                })
                .OrderBy(c => c.Categoria.Nome)
                .ToList();

            double maxGasto = resumo.Any() ? (double)resumo.Max(x => x.Total) : 100;
            ValorMaximoGauge = maxGasto;

            foreach (var cat in resumo)
            {
                CategoriasMesSelecionado.Add(cat);
            }

            var series = new List<ISeries>();
            var cores = new[]
            {
        SKColor.Parse("#2196F3"), // Azul
        SKColor.Parse("#F44336"), // Vermelho
        SKColor.Parse("#8BC34A"), // Verde
        SKColor.Parse("#FF9800"), // Laranja
        SKColor.Parse("#9C27B0"), // Roxo
        SKColor.Parse("#00BCD4")  // Ciano
    };

            for (int i = 0; i < resumo.Count; i++)
            {
                var cat = resumo[i];

                var gaugeSeries = new XamlGaugeSeries
                {
                    GaugeValue = (double)cat.Total,
                    SeriesName = cat.Categoria.Nome,
                    DataLabelsFormatter = LabelFormatter,
                    InvertedCornerRadius = true,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Start,
                    InnerRadius = 10,
                    RelativeOuterRadius = 20,
                    RelativeInnerRadius = 20,
                    Fill = new SolidColorPaint(cores[i % cores.Length]),
                    Stroke = null,
                    DataLabelsSize = 16,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    IsHoverable = true,
                    Pushout = 2
                };

                series.Add(gaugeSeries);
            }

            // Adiciona o fundo
            series.Add(new XamlGaugeBackgroundSeries
            {
                InnerRadius = 5,
                Fill = new SolidColorPaint(SKColors.LightGray.WithAlpha(100))
            });

            SeriesGraficoPizza = series;
            TotalMesAtual = mes.Total.ToString("C2");
        }

        // Se quiser criar as séries programaticamente em vez de usar o XAML


        [RelayCommand]
        private void NavegarCategorias()
        {
            var view = App.ServiceProvider.GetRequiredService<CategoriasView>();
            ViewAtual = view;
            ExibindoMenuPrincipal = false;
        }

        [RelayCommand]
        private void Voltar()
        {
            ViewAtual = null;
            ExibindoMenuPrincipal = true;
            _ = CarregarDashboardAsync();
        }

        [RelayCommand]
        private void NavegarConsumiveis()
        {
            var view = App.ServiceProvider.GetRequiredService<ConsumiveisView>();
            ViewAtual = view;
            ExibindoMenuPrincipal = false;
        }

        [RelayCommand]
        private void NavegarTransacoes()
        {
            var view = App.ServiceProvider.GetRequiredService<TransacoesView>();
            ViewAtual = view;
            ExibindoMenuPrincipal = false;
        }
    }
}