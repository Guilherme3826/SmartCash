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
using SmartCash.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        // Debug de Migrações e Carga Inicial
        using (var scope = ServiceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MeuDbContext>();

            try
            {
                // 1. Verifica quais migrações o EF Core consegue "enxergar" no Assembly
                var allMigrations = context.Database.GetMigrations().ToList();
                System.Diagnostics.Debug.WriteLine($"[EF DEBUG] Migrações encontradas no código: {string.Join(", ", allMigrations)}");

                // 2. Verifica quais migrações já estão marcadas como aplicadas no banco
                var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
                System.Diagnostics.Debug.WriteLine($"[EF DEBUG] Migrações já aplicadas no banco: {string.Join(", ", appliedMigrations)}");

                // 3. Verifica o que falta aplicar
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                System.Diagnostics.Debug.WriteLine($"[EF DEBUG] Migrações pendentes: {string.Join(", ", pendingMigrations)}");

                if (pendingMigrations.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[EF DEBUG] Aplicando migrações pendentes...");
                    context.Database.Migrate();
                    context.Database.ExecuteSqlRaw("PRAGMA journal_mode=DELETE;");
                    System.Diagnostics.Debug.WriteLine("[EF DEBUG] Migração concluída com sucesso.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[EF DEBUG] Nenhuma migração pendente encontrada.");
                }

                // Executa a carga inicial de categorias
                SeedDatabase(context);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EF ERROR] Erro ao migrar ou popular: {ex.Message}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"[EF ERROR] Inner: {ex.InnerException.Message}");
            }
        }

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
        // Verifica se já existem categorias para evitar duplicidade
        if (!context.Set<CategoriaModel>().Any())
        {
            System.Diagnostics.Debug.WriteLine("[EF DEBUG] Iniciando carga inicial de categorias...");

            var categoriasIniciais = new List<CategoriaModel>
            {
                new CategoriaModel { Nome = "Alimentação" },
                new CategoriaModel { Nome = "Transporte" },
                new CategoriaModel { Nome = "Lazer" },
                new CategoriaModel { Nome = "Saúde" },
                new CategoriaModel { Nome = "Educação" },
                new CategoriaModel { Nome = "Moradia" }
            };

            context.Set<CategoriaModel>().AddRange(categoriasIniciais);
            context.SaveChanges();

            System.Diagnostics.Debug.WriteLine("[EF DEBUG] Carga inicial concluída.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[EF DEBUG] O banco já contém categorias. Seed ignorado.");
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Define o caminho do banco (compatível com Android e Windows)
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "smartcash.db");

        // Configura o DbContext aqui, passando a string de conexão
        services.AddDbContext<MeuDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}", x => x.MigrationsAssembly("SmartCash"))); // Nome do seu projeto principal

        // Registro dos Repositórios Individuais
        services.AddScoped<IBaseRepository<CategoriaModel>, CategoriaRepository>();
        services.AddScoped<IBaseRepository<ProdutoModel>, ProdutoRepository>();
        services.AddScoped<IBaseRepository<TransacaoModel>, TransacaoRepository>();
        services.AddScoped<IBaseRepository<ItemModel>, ItemRepository>();

        // Registro das ViewModels
        services.AddSingleton<MainViewModel>(); // Singleton garante que o estado de navegação seja único
        services.AddTransient<HomeViewModel>();
        services.AddTransient<CategoriasViewModel>();

        // Registro das Views com Injeção de Dependência
        services.AddTransient<Home>();
        services.AddTransient<Categorias>();
    }
}