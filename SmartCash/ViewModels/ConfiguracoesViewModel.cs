using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartCash.EfCore.Models;
using SmartCash.Mensageiros;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace SmartCash.ViewModels
{
    public partial class ConfiguracoesViewModel : ObservableObject
    {
        private readonly string _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.json");

        [ObservableProperty] private bool _isTemaEscuro;
        [ObservableProperty] private ObservableCollection<string> _ambientesBd = new() { "Produção", "Homologação" };
        [ObservableProperty] private string _ambienteSelecionado = "Produção";

        public ConfiguracoesViewModel()
        {
            CarregarConfiguracoes();
        }

        private void CarregarConfiguracoes()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    var settings = JsonSerializer.Deserialize<AppSettingsModel>(json);

                    if (settings != null)
                    {
                        // Alteramos o campo privado para evitar disparar o OnChanged durante o carregamento
                        AmbienteSelecionado = settings.Ambiente;
                        IsTemaEscuro= settings.ModoEscuro;

                        // Aplicamos o tema manualmente uma vez no início
                        AplicarTema(IsTemaEscuro);

                        // ENVIAR MENSAGEM: Notifica o sistema que o tema foi definido/alterado
                        WeakReferenceMessenger.Default.Send(new TemaAlteradoMessage(IsTemaEscuro));

                        // Notificamos a UI sobre as mudanças
                        OnPropertyChanged(nameof(AmbienteSelecionado));
                        OnPropertyChanged(nameof(IsTemaEscuro));
                    }
                }
                catch
                {
                    /* Fallback para padrões em caso de erro no JSON */
                }
            }
        }
        /// <summary>
        /// Método disparado automaticamente pelo CommunityToolkit.Mvvm quando IsTemaEscuro muda.
        /// </summary>
        partial void OnIsTemaEscuroChanged(bool value)
        {
            AplicarTema(value);
            // ENVIAR MENSAGEM: Essencial para quando o usuário clica no toggle em tempo real
            WeakReferenceMessenger.Default.Send(new TemaAlteradoMessage(value));
        }

        private void AplicarTema(bool escuro)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = escuro ? ThemeVariant.Dark : ThemeVariant.Light;
               
            }
        }

        [RelayCommand]
        private void SalvarConfiguracoes()
        {
            try
            {
                var settings = new AppSettingsModel
                {
                    Ambiente = AmbienteSelecionado,
                    ModoEscuro = IsTemaEscuro
                };

                WeakReferenceMessenger.Default.Send(new TemaAlteradoMessage(IsTemaEscuro));

                // NOTA: Se você for rodar Native AOT, lembre-se de usar o JsonSerializerContext aqui depois
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);

                // Verifica se está rodando no Desktop (Windows, Linux, macOS)
                if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    var executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(executablePath))
                    {
                        System.Diagnostics.Process.Start(executablePath);
                    }

                    if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.Shutdown();
                    }
                }
                // Se for Android (ou iOS)
                else if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
                {
                    if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IControlledApplicationLifetime mobileLifetime)
                    {
                        try
                        {
                            mobileLifetime.Shutdown();
                        }
                        catch
                        {
                            // Ignora se o Avalonia falhar ao tentar fechar graciosamente
                        }
                    }

                    // Força bruta: Mata o processo do .NET no Android imediatamente.
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar config: {ex.Message}");
            }
        }
    }
}