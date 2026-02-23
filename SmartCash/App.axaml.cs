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
            new CategoriaModel { Nome = "Lazer", IconeApresentacao = "fa-solid fa-gamepad" },
            new CategoriaModel { Nome = "Saúde", IconeApresentacao = "fa-solid fa-heart-pulse" },
            new CategoriaModel { Nome = "Educação", IconeApresentacao = "fa-solid fa-book" },
            new CategoriaModel { Nome = "Moradia", IconeApresentacao = "fa-solid fa-house" }
        };
            context.Set<CategoriaModel>().AddRange(categorias);
            context.SaveChanges();
            System.Diagnostics.Debug.WriteLine("[EF DEBUG] Categorias populadas.");
        }

        // (Seu código de inserção de Consumíveis pode continuar aqui caso o banco esteja vazio)
        // Omiti a carga manual de consumíveis assumindo que você já rodou o script SQL de 500 itens.

        // 3. CARGA MASSIVA DE TRANSAÇÕES PARA 2025
        // Verifica se já existem transações do ano de 2025 para evitar duplicação a cada abertura do app
        if (!context.Set<TransacaoModel>().Any(t => t.Data.Year == 2025))
        {
            var consumiveisDisponiveis = context.Set<ConsumiveisModel>().ToList();

            // Só gera transações se existirem produtos cadastrados
            if (consumiveisDisponiveis.Any())
            {
                System.Diagnostics.Debug.WriteLine("[EF DEBUG] Iniciando geração de 365 dias de transações para 2025...");

                // Usamos uma seed fixa (123) para que os dados aleatórios gerados sejam sempre os mesmos
                // caso você apague o banco e recrie. Facilita os testes.
                var random = new Random(123);
                var dataInicial = new DateTime(2025, 1, 1, 8, 0, 0);

                // Loop percorrendo os 365 dias do ano
                for (int dia = 0; dia < 365; dia++)
                {
                    // Varia a hora da compra aleatoriamente entre 08:00 e 19:59
                    var dataCompra = dataInicial.AddDays(dia).AddHours(random.Next(0, 12)).AddMinutes(random.Next(0, 59));

                    var novaTransacao = new TransacaoModel
                    {
                        Data = dataCompra,
                        ValorTotal = 0 // Será calculado na soma dos itens
                    };

                    context.Set<TransacaoModel>().Add(novaTransacao);

                    // Precisamos salvar para o SQLite gerar o IdTransacao que será usado nos itens
                    context.SaveChanges();

                    int qtdItensNaCompra = random.Next(2, 6); // Cada dia terá de 2 a 5 itens diferentes
                    decimal somaValorDiario = 0;

                    for (int i = 0; i < qtdItensNaCompra; i++)
                    {
                        // Sorteia um produto aleatório da lista de 500+ consumíveis
                        var produtoSorteado = consumiveisDisponiveis[random.Next(consumiveisDisponiveis.Count)];

                        int quantidade = random.Next(1, 4); // Compra de 1 a 3 unidades do mesmo produto
                        decimal valorTotalItem = produtoSorteado.Valor * quantidade;

                        var novoItem = new ItemModel
                        {
                            IdTransacao = novaTransacao.IdTransacao,
                            IdConsumivel = produtoSorteado.IdConsumivel,
                            Quantidade = quantidade,
                            ValorUnit = produtoSorteado.Valor,
                            ValorTotal = valorTotalItem
                        };

                        context.Set<ItemModel>().Add(novoItem);
                        somaValorDiario += valorTotalItem;
                    }

                    // Atualiza o cabeçalho da transação com o valor total correto
                    novaTransacao.ValorTotal = somaValorDiario;
                    context.Set<TransacaoModel>().Update(novaTransacao);
                }

                // Salva todas as atualizações de ValorTotal e todos os Itens de uma vez
                context.SaveChanges();
                System.Diagnostics.Debug.WriteLine("[EF DEBUG] Geração de transações de 2025 concluída com sucesso!");
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
        services.AddTransient<ITransacaoRepository, TransacaoRepository>();
        services.AddTransient<IItemRepository, ItemRepository>();

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