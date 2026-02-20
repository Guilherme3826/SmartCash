using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.EfCore;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;
using SmartCash.EfCore.Repositories;
using SmartCash.ViewModels;
using SmartCash.Views;
using System;
using System.IO;

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
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        // Garante a criação/migração do banco de dados na inicialização
        using (var scope = ServiceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MeuDbContext>();
            context.Database.Migrate();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Define o caminho do banco (compatível com Android e Windows)
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "smartcash.db");

        // Configura o DbContext aqui, passando a string de conexão
        services.AddDbContext<MeuDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Registro dos Repositórios Individuais
        services.AddScoped<IBaseRepository<CategoriaModel>, CategoriaRepository>();
        services.AddScoped<IBaseRepository<ProdutoModel>, ProdutoRepository>();
        services.AddScoped<IBaseRepository<TransacaoModel>, TransacaoRepository>();
        services.AddScoped<IBaseRepository<ItemModel>, ItemRepository>();

        // ViewModels
        services.AddTransient<MainViewModel>();
    }
}