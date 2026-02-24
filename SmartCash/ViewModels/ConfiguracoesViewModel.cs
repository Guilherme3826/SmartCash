using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartCash.EfCore;
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

                    // AOT CORRECTION: Usando o Source Generator Context definido anteriormente
                    var settings = JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettingsModel);

                    if (settings != null)
                    {
                        // MVVM Toolkit CORRECTION: Atribuindo aos campos para evitar loops de evento, 
                        // mas garantindo que o estado interno do gerador de código seja respeitado
                        _ambienteSelecionado = settings.Ambiente;
                        _isTemaEscuro = settings.ModoEscuro;

                        AplicarTema(IsTemaEscuro);

                        WeakReferenceMessenger.Default.Send(new TemaAlteradoMessage(IsTemaEscuro));

                        // Notificamos a UI manualmente já que alteramos os campos privados
                        OnPropertyChanged(nameof(AmbienteSelecionado));
                        OnPropertyChanged(nameof(IsTemaEscuro));
                    }
                }
                catch
                {
                    /* Fallback para padrões */
                }
            }
        }

        partial void OnIsTemaEscuroChanged(bool value)
        {
            AplicarTema(value);
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

                // AOT CORRECTION: Removido 'new JsonSerializerOptions'. 
                // Usamos o Context para serialização nativa sem reflexão.
                string json = JsonSerializer.Serialize(settings, AppSettingsContext.Default.AppSettingsModel);

                File.WriteAllText(_configPath, json);
            }
            catch (Exception)
            {
                // Log ou notificação
            }
        }
    }
}