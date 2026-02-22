using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using SmartCash.EfCore;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.EfCore.Repositories;
using SmartCash.ViewModels;
using SmartCash.ViewModels.Categorias;
using SmartCash.ViewModels.Consumiveis;
using SmartCash.ViewModels.Transacoes;
using SmartCash.Views;
using SmartCash.Views.Categorias;
using SmartCash.Views.Consumiveis;
using SmartCash.Views.Transacoes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCash;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        // Executa migrações e Seed em segundo plano para não travar a UI no Android
        Task.Run(async () =>
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MeuDbContext>();

                try
                {
                    // Aplica migrações pendentes de forma assíncrona
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("[EF DEBUG] Aplicando migrações pendentes...");
                        await context.Database.MigrateAsync();
                        // Garante que o SQLite finalize a escrita imediatamente
                        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;");
                    }

                    // Popula o banco de dados seguindo a ordem lógica
                    SeedDatabase(context);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EF ERROR] Erro ao processar banco: {ex.Message}");
                }
            }
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = ServiceProvider.GetRequiredService<MainViewModel>() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView { DataContext = ServiceProvider.GetRequiredService<MainViewModel>() };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SeedDatabase(MeuDbContext context)
    {
        // 1. CARGA DE CATEGORIAS
        if (!context.Set<CategoriaModel>().Any())
        {
            var categorias = new List<CategoriaModel>
            {
                new CategoriaModel { Nome = "Alimentação", IconeApresentacao = "fa-solid fa-utensils" },
                new CategoriaModel { Nome = "Transporte", IconeApresentacao = "fa-solid fa-car" },
                new CategoriaModel { Nome = "Lazer", IconeApresentacao = "fa-solid fa-gamepad" }
            };
            context.Set<CategoriaModel>().AddRange(categorias);
            context.SaveChanges();
            System.Diagnostics.Debug.WriteLine("[EF DEBUG] Categorias populadas.");
        }

        // 2. CARGA DE CONSUMÍVEIS
        if (!context.Set<ConsumiveisModel>().Any())
        {
            var catAlimentacao = context.Set<CategoriaModel>().FirstOrDefault(c => c.Nome == "Alimentação");
            var catTransporte = context.Set<CategoriaModel>().FirstOrDefault(c => c.Nome == "Transporte");

            if (catAlimentacao != null && catTransporte != null)
            {
                var consumiveis = new List<ConsumiveisModel>
                {
                    new ConsumiveisModel { Nome = "Mercado Mensal", Valor = 450.00m, IdCategoria = catAlimentacao.IdCategoria },
                    new ConsumiveisModel { Nome = "Combustível", Valor = 200.00m, IdCategoria = catTransporte.IdCategoria }
                };
                context.Set<ConsumiveisModel>().AddRange(consumiveis);
                context.SaveChanges();
                System.Diagnostics.Debug.WriteLine("[EF DEBUG] Consumíveis populados.");
            }
        }

        // 3. CARGA DE TRANSAÇÕES (Cabeçalho)
        if (!context.Set<TransacaoModel>().Any())
        {
            var transacoes = new List<TransacaoModel>
            {
                new TransacaoModel { Data = new DateTime(2026, 2, 15, 14, 30, 0), ValorTotal = 450.00m },
                new TransacaoModel { Data = new DateTime(2026, 1, 20, 10, 0, 0), ValorTotal = 200.00m }
            };

            context.Set<TransacaoModel>().AddRange(transacoes);
            context.SaveChanges();
            System.Diagnostics.Debug.WriteLine("[EF DEBUG] Tabela Transacao populada.");
        }

        // 4. CARGA DE ITENS (Relacional)
        if (!context.Set<ItemModel>().Any())
        {
            var tFevereiro = context.Set<TransacaoModel>().FirstOrDefault(t => t.Data.Month == 2);
            var tJaneiro = context.Set<TransacaoModel>().FirstOrDefault(t => t.Data.Month == 1);
            var prodMercado = context.Set<ConsumiveisModel>().FirstOrDefault(p => p.Nome == "Mercado Mensal");
            var prodCombustivel = context.Set<ConsumiveisModel>().FirstOrDefault(p => p.Nome == "Combustível");

            if (tFevereiro != null && tJaneiro != null && prodMercado != null && prodCombustivel != null)
            {
                var itens = new List<ItemModel>
                {
                    new ItemModel { IdTransacao = tFevereiro.IdTransacao, IdConsumivel = prodMercado.IdConsumivel, Quantidade = 1, ValorUnit = 450.00m, ValorTotal = 450.00m },
                    new ItemModel { IdTransacao = tJaneiro.IdTransacao, IdConsumivel = prodCombustivel.IdConsumivel, Quantidade = 1, ValorUnit = 200.00m, ValorTotal = 200.00m }
                };

                context.Set<ItemModel>().AddRange(itens);
                context.SaveChanges();
                System.Diagnostics.Debug.WriteLine("[EF DEBUG] Tabela Item populada com sucesso.");
            }
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Caminho do banco para Android
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "smartcash.db");

        services.AddDbContextFactory<MeuDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}", x => x.MigrationsAssembly("SmartCash")));

        // Repositórios
        services.AddTransient<IBaseRepository<CategoriaModel>, CategoriaRepository>();
        services.AddTransient<IBaseRepository<ConsumiveisModel>, ConsumivelRepository>();
        services.AddTransient<IBaseRepository<TransacaoModel>, TransacaoRepository>();
        services.AddTransient<IBaseRepository<ItemModel>, ItemRepository>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<CategoriasViewModel>();
        services.AddSingleton<ConsumiveisViewModel>();
        services.AddSingleton<TransacoesViewModel>();

        services.AddTransient<AdicionarCategoriaViewModel>();
        services.AddTransient<AdicionarTransacaoViewModel>();
        services.AddTransient<TransacaoDetalhesViewModel>();
        services.AddTransient<AdicionarConsumivelViewModel>();

        // Views
        services.AddTransient<CategoriasView>();
        services.AddTransient<AdicionarCategoriaView>();
        services.AddTransient<ConsumiveisView>();
        services.AddTransient<TransacoesView>();
        services.AddTransient<TransacaoDetalhesView>();
        services.AddTransient<AdicionarTransacaoView>();
        services.AddTransient<AdicionarConsumivelView>();

    }
}