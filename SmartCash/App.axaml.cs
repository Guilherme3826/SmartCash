using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
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
using SmartCash.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartCash
{
    public partial class App : Application
    {
        public static Action<IServiceCollection>? RegisterPlatformServices { get; set; }
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        private AppSettingsModel _configuracoesAtuais = new AppSettingsModel { Ambiente = "Produção", ModoEscuro = false };

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            IconProvider.Current.Register<FontAwesomeIconProvider>();

            CarregarConfiguracoes();

            var services = new ServiceCollection();
            ConfigureServices(services);

            RegisterPlatformServices?.Invoke(services);

            ServiceProvider = services.BuildServiceProvider();

            Task.Run(async () =>
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<MeuDbContext>();

                    try
                    {
                        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");

                        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                        if (pendingMigrations.Any())
                        {
                            System.Diagnostics.Debug.WriteLine("[EF DEBUG] Aplicando migrações pendentes...");
                            await context.Database.MigrateAsync();
                        }

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

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = _configuracoesAtuais.ModoEscuro
                    ? Avalonia.Styling.ThemeVariant.Dark
                    : Avalonia.Styling.ThemeVariant.Light;

                WeakReferenceMessenger.Default.Send(new TemaAlteradoMessage(_configuracoesAtuais.ModoEscuro));
            }
        }

        private void SeedDatabase(MeuDbContext context)
        {
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
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var settings = JsonSerializer.Deserialize<AppSettingsModel>(json, options);

                    if (settings != null)
                    {
                        _configuracoesAtuais = settings;
                    }
                }
                catch
                {
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

            // Repositórios permanecem com registro explícito porque mapeiam Interfaces genéricas para Implementações
            services.AddTransient<IBaseRepository<CategoriaModel>, CategoriaRepository>();
            services.AddTransient<IBaseRepository<ConsumiveisModel>, ConsumivelRepository>();
            services.AddTransient<ITransacaoRepository, TransacaoRepository>();
            services.AddTransient<IItemRepository, ItemRepository>();

            // Implementação por Reflection para automatizar Views e ViewModels
            var assembly = Assembly.GetExecutingAssembly();
            var todosOsTipos = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract);

            // Lista de exceção para garantir que as ViewModels de navegação persistam seus dados
            var singletonsExigidos = new HashSet<string>
            {
                "MainViewModel",
                "CategoriasViewModel",
                "ConsumiveisViewModel",
                "TransacoesViewModel",
                "ConfiguracoesViewModel",
                "DashboardViewModel"
            };

            foreach (var tipo in todosOsTipos)
            {
                if (tipo.Name.EndsWith("ViewModel"))
                {
                    if (singletonsExigidos.Contains(tipo.Name))
                    {
                        services.AddSingleton(tipo);
                    }
                    else
                    {
                        services.AddTransient(tipo);
                    }
                }
                else if (tipo.Name.EndsWith("View") && tipo.IsSubclassOf(typeof(Avalonia.Controls.UserControl)))
                {
                    services.AddTransient(tipo);
                }
            }
        }
    }
}