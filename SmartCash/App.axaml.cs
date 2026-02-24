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
using System.Text.Json;

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

        // Inicializa o banco ANTES de chamar as Views, de forma síncrona/esperada pelo AOT.
        // Isso evita que o EF Core se perca em threads dinâmicas.
        InitializeDatabase();

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

    private void InitializeDatabase()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MeuDbContext>();

            // EnsureCreated é o método correto e seguro para AOT (não usa Migrations/Reflection)
            bool criadoAgora = context.Database.EnsureCreated();

            if (criadoAgora)
            {
                System.Diagnostics.Debug.WriteLine("[EF DEBUG] Banco criado via EnsureCreated.");
                context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            }

            // A chamada síncrona aqui garante que o LINQ seja pré-compilado pelo AOT
            SeedDatabase(context);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EF ERROR] Erro ao inicializar banco: {ex.Message}");
        }
    }

    private void SeedDatabase(MeuDbContext context)
    {
        // Usamos ADO.NET bruto para o Seed. 
        // Isso evita o bug do EF Core Generator na inicialização do Avalonia.

        bool hasCategorias = false;

        try
        {
            using var connection = context.Database.GetDbConnection();
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }

            // 1. CHECA SE EXISTEM CATEGORIAS (Substitui o .Any())
            using (var checkCommand = connection.CreateCommand())
            {
                // Verifica o nome exato da sua tabela no banco gerado. 
                // Geralmente é "Categorias" ou "CategoriaModel".
                checkCommand.CommandText = "SELECT 1 FROM Categorias LIMIT 1;";

                using (var reader = checkCommand.ExecuteReader())
                {
                    hasCategorias = reader.HasRows;
                }
            }

            // 2. INSERE AS CATEGORIAS SE NÃO EXISTIREM
            if (!hasCategorias)
            {
                using (var insertCommand = connection.CreateCommand())
                {
                    // Inserção em massa direta e extremamente rápida
                    insertCommand.CommandText = @"
                        INSERT INTO Categoria (Nome, IconeApresentacao) VALUES 
                        ('Alimentação', 'fa-solid fa-utensils'),
                        ('Transporte', 'fa-solid fa-car'),
                        ('Lazer', 'fa-solid fa-gamepad'),
                        ('Saúde', 'fa-solid fa-heart-pulse'),
                        ('Educação', 'fa-solid fa-book'),
                        ('Moradia', 'fa-solid fa-house');
                    ";

                    insertCommand.ExecuteNonQuery();
                }

                System.Diagnostics.Debug.WriteLine("[EF DEBUG] Categorias populadas via SQL.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EF ERROR] Erro no Seed de Banco de Dados: {ex.Message}");
        }
    }

    private string GetDatabasePath()
    {
        string pastaLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string configPath = Path.Combine(pastaLocal, "settings.json");
        string ambiente = "Produção";

        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);

                // CORREÇÃO AOT: Usando o Source Generator Context em vez de Reflection
                var settings = JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettingsModel);

                ambiente = settings?.Ambiente ?? "Produção";
            }
            catch
            {
                /* Se o JSON estiver corrompido, mantém o fallback para Produção */
            }
        }

        string nomeBanco = (ambiente == "Homologação") ? "smartcash_homolog.db" : "smartcash.db";
        return Path.Combine(pastaLocal, nomeBanco);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContextFactory<MeuDbContext>(options =>
        {
            options.UseSqlite($"Data Source={GetDatabasePath()}", x => x.MigrationsAssembly("SmartCash"))
                   .UseModel(SmartCash.CompiledModels.MeuDbContextModel.Instance); // Ativa o modelo compilado para AOT
        });

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
        services.AddSingleton<ConfiguracoesViewModel>();

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
        services.AddTransient<ConfiguracoesView>();
    }
}