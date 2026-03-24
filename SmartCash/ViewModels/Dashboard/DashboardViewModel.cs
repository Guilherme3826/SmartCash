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
using System.Globalization;
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

        // Propriedade adicionada para capturar o clique do usuário na lista
        [ObservableProperty]
        private ResumoCategoriaModel? _categoriaSelecionada;

        [ObservableProperty]
        private IEnumerable<ISeries> _seriesGraficoPizza = Enumerable.Empty<ISeries>();

        [ObservableProperty]
        private double _valorMaximoGauge = 100;

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

            WeakReferenceMessenger.Default.Register<NovaTransacaoAdicionada>(this, (r, m) =>
            {
                _ = CarregarDashboardAsync();
            });


            _ = CarregarDashboardAsync();
        }

        partial void OnMesSelecionadoChanged(ResumoMesModel? value)
        {
            if (value != null)
            {
                _ = CarregarItensDoMesSelecionadoAsync(value);
            }
        }

        // Evento disparado automaticamente ao clicar em uma categoria na lista do Dashboard
        partial void OnCategoriaSelecionadaChanged(ResumoCategoriaModel? value)
        {
            if (value != null)
            {
                // Formata o período do Dashboard para o formato de texto que a CategoriaDetalhesViewModel espera (ex: "março 2026")
                string filtroAtual = MesSelecionado != null
                    ? new DateTime(MesSelecionado.Ano, MesSelecionado.Mes, 1).ToString("MMMM yyyy", new CultureInfo("pt-BR"))
                    : "Todos os Períodos";

                // Envia a mensagem global com o ID e o Filtro formatado
                WeakReferenceMessenger.Default.Send(new NavegarParaCategoriaDetalhesGlobalMessage(value.Categoria.IdCategoria, filtroAtual));

                // Limpa a seleção para permitir que o usuário clique no mesmo item novamente depois
                CategoriaSelecionada = null;
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
                .OrderByDescending(c => c.Total)
                .Take(8)
                .ToList();

            double maxGasto = resumo.Any() ? (double)resumo.Max(x => x.Total) : 100;
            ValorMaximoGauge = maxGasto;

            var coresHex = new[]
            {
                "#2196F3",
                "#F44336",
                "#8BC34A",
                "#FF9800",
                "#9C27B0",
                "#00BCD4",
                "#E91E63",
                "#FFC107"
            };

            for (int i = 0; i < resumo.Count; i++)
            {
                var cat = resumo[i];
                cat.CorHex = coresHex[i % coresHex.Length];
                CategoriasMesSelecionado.Add(cat);
            }

            var series = new List<ISeries>();

            for (int i = resumo.Count - 1; i >= 0; i--)
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