using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.Desktop.Services; // Namespace onde você criou o WindowsCompartilhamentoService
using System;

namespace SmartCash.Desktop
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Injeta o serviço específico do Windows antes do App rodar
            App.RegisterPlatformServices = services =>
            {
                services.AddSingleton<Interfaces.ICompartilhamentoService, WindowsCompartilhamentoService>();
                services.AddSingleton<Interfaces.IPermissaoService, WindowsPermissaoService>();
            };

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}