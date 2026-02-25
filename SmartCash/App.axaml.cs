using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using SmartCash.EfCore;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.EfCore.Repositories;
using SmartCash.Mensageiros;
using SmartCash.ViewModels;
using SmartCash.ViewModels.Categorias;
using SmartCash.ViewModels.Consumiveis;
using SmartCash.ViewModels.Dashboard;
using SmartCash.ViewModels.Transacoes;
using SmartCash.Views;
using SmartCash.Views.Categorias;
using SmartCash.Views.Consumiveis;
using SmartCash.Views.Dashboard;
using SmartCash.Views.Transacoes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartCash;

public partial class App : Application
{
    public static Action<IServiceCollection>? RegisterPlatformServices { get; set; }
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    // Armazena as configurações na memória para serem usadas por todo o ciclo de vida
    private AppSettingsModel _configuracoesAtuais = new AppSettingsModel { Ambiente = "Produção", ModoEscuro = false };

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();

        // 1. Ler o arquivo de configuração
        CarregarConfiguracoes();

        // 2. Criar UMA ÚNICA coleção de serviços
        var services = new ServiceCollection();
        ConfigureServices(services);

        // 3. Registar os serviços da plataforma (Android/Desktop)
        RegisterPlatformServices?.Invoke(services);

        // 4. CONSTRUIR o ServiceProvider SÓ AGORA (depois de todos os serviços registados)
        ServiceProvider = services.BuildServiceProvider();

        // Executa migrações e Seed em segundo plano para não travar a UI no Android
        Task.Run(async () =>
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MeuDbContext>();

                try
                {
                    // 1. Antes de tudo, força o SQLite a descartar logs antigos e sincronizar
                    // Isso resolve o problema de ler dados "fantasmas" que estavam no -wal antigo do celular
                    await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");

                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("[EF DEBUG] Aplicando migrações pendentes...");
                        await context.Database.MigrateAsync();
                    }

                    // 2. Garante que o modo de jornal seja compatível com a sincronização via ADB
                    // DELETE faz o SQLite apagar o -wal e o -shm sempre que fecha a conexão,
                    // facilitando muito a sua vida ao substituir o arquivo .db
                    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;");

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

        // 2. Aplicar o tema na thread principal (UI) imediatamente após carregar o framework
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = _configuracoesAtuais.ModoEscuro
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;

            // Envia a mensagem para que o Android e outras Views saibam do tema inicial
            WeakReferenceMessenger.Default.Send(new TemaAlteradoMessage(_configuracoesAtuais.ModoEscuro));
        }


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
    }

    private void CarregarConfiguracoes()
    {
        string pastaLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string configPath = Path.Combine(pastaLocal, "settings.json");

        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);

                // Propriedade vital: Evita que o JSON falhe silenciosamente caso a serialização tenha alterado a caixa das letras (Maiúsculo/Minúsculo)
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var settings = JsonSerializer.Deserialize<AppSettingsModel>(json, options);

                if (settings != null)
                {
                    _configuracoesAtuais = settings;
                }
            }
            catch
            {
                /* Se falhar ou estiver corrompido, a variável _configuracoesAtuais manterá o padrão Produção/Light configurado no topo */
            }
        }
    }

    private string GetDatabasePath()
    {
        string pastaLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string nomeBanco = _configuracoesAtuais.Ambiente == "Homologação" ? "smartcash_homolog.db" : "smartcash.db";
        return Path.Combine(pastaLocal, nomeBanco);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContextFactory<MeuDbContext>(options =>
                options.UseSqlite($"Data Source={GetDatabasePath()}", x => x.MigrationsAssembly("SmartCash")));

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
        services.AddSingleton<DashboardViewModel>();

        services.AddTransient<AdicionarCategoriaViewModel>();
        services.AddTransient<AdicionarTransacaoViewModel>();
        services.AddTransient<TransacaoDetalhesViewModel>();
        services.AddTransient<AdicionarConsumivelViewModel>();

        // Views
        services.AddTransient<DashboardView>();
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