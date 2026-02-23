using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using SmartCash.Views;
using SmartCash.Views.Categorias;
using SmartCash.Views.Consumiveis;
using SmartCash.Views.Transacoes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        [ObservableProperty]
        bool isTemaEscuro = false;

        private LvcColor _graficoBackground;
        public LvcColor GraficoBackground
        {
            get => _graficoBackground;
            set => SetProperty(ref _graficoBackground, value);
        }

        public Func<ChartPoint, string> LabelFormatter { get; set; }

        public MainViewModel(ITransacaoRepository transacaoRepository, IItemRepository itemRepository)
        {
            WeakReferenceMessenger.Default.Register<TemaAlteradoMessage>(this, (r, m) =>
            {
                IsTemaEscuro = m.Value;
                // m.Value contém o bool isDark enviado
                AtualizarCoresDoTemaDoGrafico(IsTemaEscuro);
            });

            if (Application.Current != null)
            {
                IsTemaEscuro = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
            }

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
                    // DataLabelsPaint foi removido daqui e transferido para a lógica central de tema
                };

                series.Add(gaugeSeries);
            }

            series.Add(new XamlGaugeBackgroundSeries
            {
                InnerRadius = 5
                // Fill foi removido daqui e transferido para a lógica central de tema
            });

            SeriesGraficoPizza = series;
            TotalMesAtual = mes.Total.ToString("C2");

            // Chama o método para aplicar as cores do tema logo após a criação da série
            AtualizarCoresDoTemaDoGrafico(IsTemaEscuro);
        }

        private void AtualizarCoresDoTemaDoGrafico(bool isTemaEscuro)
        {
            Debug.WriteLine($"Evento de alteração de cor do gráfico ativado cor: Escura {IsTemaEscuro}");

            if (SeriesGraficoPizza == null || !SeriesGraficoPizza.Any()) return;

            // 1. Fundo do controle de gráfico (O "quadrado")
            GraficoBackground = isTemaEscuro
                ? LvcColor.FromArgb(255, 17, 27, 33)   // WaSurface Dark (#111B21)
                : LvcColor.FromArgb(255, 255, 255, 255); // Branco puro no Light

            // 2. Cor do texto das legendas
            var corTextoLegenda = isTemaEscuro
                ? new SolidColorPaint(new SKColor(233, 237, 239)) // Branco Gelo opaco
                : new SolidColorPaint(new SKColor(17, 27, 33));   // WaSurface opaco

            // 3. Cor do trilho (fundo) do Gauge
            var corFundoGrafico = isTemaEscuro
                ? new SolidColorPaint(new SKColor(31, 44, 52).WithAlpha(100)) // WaPrimary com transparência
                : new SolidColorPaint(SKColors.LightGray.WithAlpha(100));     // Cinza claro original

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

        // Se quiser criar as séries programaticamente em vez de usar o XAML


        [RelayCommand]
        private void NavegarCategorias()
        {
            var view = App.ServiceProvider.GetRequiredService<CategoriasView>();
            ViewAtual = view;
            ExibindoMenuPrincipal = false;
        }

        [RelayCommand]
        private async Task Voltar()
        {
            // 1. Volta para a tela principal
            ViewAtual = null;
            ExibindoMenuPrincipal = true;

            // 2. Limpa a lista para destruir o cache visual antigo do LiveCharts
            SeriesGraficoPizza = Enumerable.Empty<ISeries>();

            // 3. Dá um respiro de 50ms para a UI Thread do Avalonia aplicar 
            // a visibilidade (IsVisible="True") antes de desenharmos o gráfico
            await Task.Delay(50);

            // 4. Recarrega os dados do cabeçalho
            await CarregarDashboardAsync();

            // 5. Reconstrói o gráfico do zero, agora com a tela já visível
            if (MesSelecionado != null)
            {
                await CarregarItensDoMesSelecionadoAsync(MesSelecionado);
            }
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

        [RelayCommand]
        private void NavegarConfiguracoes()
        {
            var view = App.ServiceProvider.GetRequiredService<ConfiguracoesView>();
            ViewAtual = view;
            ExibindoMenuPrincipal = false;
        }
    }
}