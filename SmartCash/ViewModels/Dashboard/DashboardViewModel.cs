using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Dashboard
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly ITransacaoRepository _transacaoRepository;
        private readonly IItemRepository _itemRepository;

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

        [ObservableProperty]
        private double _vanesa;

        [ObservableProperty]
        private double _charles;

        [ObservableProperty]
        private double _ana;

        [ObservableProperty]
        bool isTemaEscuro = false;

        private LvcColor _graficoBackground;
        public LvcColor GraficoBackground
        {
            get => _graficoBackground;
            set => SetProperty(ref _graficoBackground, value);
        }

        public Func<ChartPoint, string> LabelFormatter { get; set; }

        public DashboardViewModel(ITransacaoRepository transacaoRepository, IItemRepository itemRepository)
        {
            WeakReferenceMessenger.Default.Register<TemaAlteradoMessage>(this, (r, m) =>
            {
                IsTemaEscuro = m.Value;
                AtualizarCoresDoTemaDoGrafico(IsTemaEscuro);
            });

            if (Application.Current != null)
            {
                IsTemaEscuro = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
            }

            _transacaoRepository = transacaoRepository;
            _itemRepository = itemRepository;

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
            var coresHex = new[]
            {
                "#2196F3", // Azul
                "#F44336", // Vermelho
                "#8BC34A", // Verde
                "#FF9800", // Laranja
                "#9C27B0", // Roxo
                "#00BCD4"  // Ciano
            };

            for (int i = 0; i < resumo.Count; i++)
            {
                var cat = resumo[i];
                cat.CorHex = coresHex[i % coresHex.Length];

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
                    Fill = new SolidColorPaint(SKColor.Parse(cat.CorHex)),
                    Stroke = null,
                    DataLabelsSize = 12,
                    IsHoverable = true,
                    Pushout = 2
                };

                series.Add(gaugeSeries);
            }

            series.Add(new XamlGaugeBackgroundSeries
            {
                InnerRadius = 5
            });

            SeriesGraficoPizza = series;
            TotalMesAtual = mes.Total.ToString("C2");

            AtualizarCoresDoTemaDoGrafico(IsTemaEscuro);
        }

        private void AtualizarCoresDoTemaDoGrafico(bool isTemaEscuro)
        {
            Debug.WriteLine($"Evento de alteração de cor do gráfico ativado cor: Escura {IsTemaEscuro}");

            if (SeriesGraficoPizza == null || !SeriesGraficoPizza.Any()) return;

            GraficoBackground = isTemaEscuro
                ? LvcColor.FromArgb(255, 17, 27, 33)
                : LvcColor.FromArgb(255, 255, 255, 255);

            var corTextoLegenda = isTemaEscuro
                ? new SolidColorPaint(new SKColor(233, 237, 239))
                : new SolidColorPaint(new SKColor(17, 27, 33));

            var corFundoGrafico = isTemaEscuro
                ? new SolidColorPaint(new SKColor(31, 44, 52).WithAlpha(100))
                : new SolidColorPaint(SKColors.LightGray.WithAlpha(100));

            foreach (var serie in SeriesGraficoPizza)
            {
                if (serie is XamlGaugeSeries gaugeSeries)
                {
                    gaugeSeries.DataLabelsPaint = corTextoLegenda;
                }
                else if (serie is XamlGaugeBackgroundSeries bgSeries)
                {
                    bgSeries.Fill = corFundoGrafico;
                }
            }
        }
    }
}