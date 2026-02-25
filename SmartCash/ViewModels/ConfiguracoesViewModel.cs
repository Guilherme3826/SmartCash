using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.EfCore.Models;
using SmartCash.Interfaces;
using SmartCash.Mensageiros;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartCash.ViewModels
{
    public partial class ConfiguracoesViewModel : ObservableObject
    {
        private readonly string _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.json");
        private bool _isCarregando = true;

        [ObservableProperty] private bool _isTemaEscuro;
        [ObservableProperty] private ObservableCollection<string> _ambientesBd = new() { "Produção", "Homologação" };
        [ObservableProperty] private string _ambienteSelecionado = "Produção";

        private readonly ICompartilhamentoService? _compartilhamentoService;
        private readonly IPermissaoService? _permissaoService;

        public ConfiguracoesViewModel()
        {
            _compartilhamentoService = App.ServiceProvider.GetService<ICompartilhamentoService>();
            _permissaoService = App.ServiceProvider.GetService<IPermissaoService>();

            CarregarConfiguracoes();
            _isCarregando = false;
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
                        AmbienteSelecionado = settings.Ambiente;
                        IsTemaEscuro = settings.ModoEscuro;

                        AplicarTema(IsTemaEscuro);
                        WeakReferenceMessenger.Default.Send(new TemaAlteradoMessage(IsTemaEscuro));

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

            if (!_isCarregando)
            {
                SalvarConfiguracoesNoArquivo();
            }
        }

        partial void OnAmbienteSelecionadoChanged(string value)
        {
            if (_isCarregando) return;

            SalvarConfiguracoesNoArquivo();
            ReiniciarAplicativo();
        }

        private void AplicarTema(bool escuro)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = escuro ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }

        private void SalvarConfiguracoesNoArquivo()
        {
            try
            {
                var settings = new AppSettingsModel
                {
                    Ambiente = AmbienteSelecionado,
                    ModoEscuro = IsTemaEscuro
                };

                Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
                File.WriteAllText(_configPath, JsonSerializer.Serialize(settings));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar config: {ex.Message}");
            }
        }

        private void ReiniciarAplicativo()
        {
            if (OperatingSystem.IsWindows())
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(processPath) { UseShellExecute = true });
                }
            }

            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IControlledApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }

            Environment.Exit(0);
        }

        [RelayCommand]
        private async Task RestaurarBancoDados()
        {
            if (_permissaoService != null)
            {
                bool temPermissao = await _permissaoService.SolicitarPermissaoArmazenamentoAsync();
                if (!temPermissao) return;
            }

            try
            {
                string diretorioBackup = OperatingSystem.IsWindows()
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SmartCash_Backups")
                    : OperatingSystem.IsAndroid()
                        ? "/storage/emulated/0/Download/SmartCash_Backups"
                        : AppContext.BaseDirectory;

                string caminhoZip = Path.Combine(diretorioBackup, "SmartCashDbBackups.zip");

                if (!File.Exists(caminhoZip))
                {
                    System.Diagnostics.Debug.WriteLine("Arquivo não encontrado para restaurar.");
                    return;
                }

                string pastaSandbox = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                ZipFile.ExtractToDirectory(caminhoZip, pastaSandbox, overwriteFiles: true);

                ReiniciarAplicativo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao restaurar: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task BackupBancoDados()
        {
            if (_permissaoService != null)
            {
                bool temPermissao = await _permissaoService.SolicitarPermissaoArmazenamentoAsync();
                if (!temPermissao) return;
            }

            try
            {
                string diretorioBackup = OperatingSystem.IsWindows()
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SmartCash_Backups")
                    : OperatingSystem.IsAndroid()
                        ? "/storage/emulated/0/Download/SmartCash_Backups"
                        : AppContext.BaseDirectory;

                if (!Directory.Exists(diretorioBackup)) Directory.CreateDirectory(diretorioBackup);

                string caminhoZipDestino = Path.Combine(diretorioBackup, "SmartCashDbBackups.zip");

                string pastaSandbox = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string idUnico = Guid.NewGuid().ToString();
                string pastaTemp = Path.Combine(Path.GetTempPath(), "SmartCashBackupTemp_" + idUnico);
                string arquivoZipTemp = Path.Combine(Path.GetTempPath(), "TempZip_" + idUnico + ".zip");

                Directory.CreateDirectory(pastaTemp);

                var arquivosNaSandbox = Directory.GetFiles(pastaSandbox);
                foreach (var arquivo in arquivosNaSandbox)
                {
                    if (arquivo.EndsWith(".db") || arquivo.EndsWith(".db-wal") || arquivo.EndsWith(".db-shm"))
                    {
                        File.Copy(arquivo, Path.Combine(pastaTemp, Path.GetFileName(arquivo)), true);
                    }
                }

                // Cria o zip no diretório temporário para evitar conflitos de System.IO
                ZipFile.CreateFromDirectory(pastaTemp, arquivoZipTemp);

                // Move para a pasta pública forçando a sobrescrita nativa
                File.Copy(arquivoZipTemp, caminhoZipDestino, true);

                // Limpeza
                Directory.Delete(pastaTemp, true);
                File.Delete(arquivoZipTemp);

                System.Diagnostics.Debug.WriteLine("Backup concluído com sucesso em: " + caminhoZipDestino);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao fazer backup: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ExportarBancoDados()
        {
            if (_permissaoService != null)
            {
                bool temPermissao = await _permissaoService.SolicitarPermissaoArmazenamentoAsync();
                if (!temPermissao) return;
            }

            try
            {
                string diretorioBackup = OperatingSystem.IsWindows()
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SmartCash_Backups")
                    : OperatingSystem.IsAndroid()
                        ? "/storage/emulated/0/Download/SmartCash_Backups"
                        : AppContext.BaseDirectory;

                string caminhoZip = Path.Combine(diretorioBackup, "SmartCashDbBackups.zip");

                if (!File.Exists(caminhoZip))
                {
                    System.Diagnostics.Debug.WriteLine("Arquivo de backup não encontrado.");
                    return;
                }

                if (_compartilhamentoService != null)
                {
                    if (OperatingSystem.IsAndroid())
                    {
                        _compartilhamentoService.CompartilharArquivo(caminhoZip, "Backup SmartCash");
                    }
                    else if (OperatingSystem.IsWindows())
                    {
                        _compartilhamentoService.AbrirPastaDoArquivo(caminhoZip);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao exportar: {ex.Message}");
            }
        }
    }
}