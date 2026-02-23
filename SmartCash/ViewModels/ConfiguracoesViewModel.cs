using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartCash.EfCore.Models;
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
                        _ambienteSelecionado = settings.Ambiente;
                        _isTemaEscuro = settings.ModoEscuro;

                        // Aplicamos o tema manualmente uma vez no início
                        AplicarTema(_isTemaEscuro);

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

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception)
            {
                // Aqui você pode adicionar um log ou notificação de erro ao salvar
            }
        }
    }
}