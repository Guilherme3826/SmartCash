using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
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

        public ConfiguracoesViewModel()
        {
            _compartilhamentoService = App.ServiceProvider.GetService<ICompartilhamentoService>();
            // Não precisamos mais do serviço de permissão nativo!
            CarregarConfiguracoes();
            _isCarregando = false;
        }

        // --- Lógica de Temas e Configurações (Mantida Intacta) ---
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
                catch { /* Fallback para padrões */ }
            }
        }

        partial void OnIsTemaEscuroChanged(bool value)
        {
            AplicarTema(value);
            WeakReferenceMessenger.Default.Send(new TemaAlteradoMessage(value));
            if (!_isCarregando) SalvarConfiguracoesNoArquivo();
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
                app.RequestedThemeVariant = escuro ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        private void SalvarConfiguracoesNoArquivo()
        {
            try
            {
                var settings = new AppSettingsModel { Ambiente = AmbienteSelecionado, ModoEscuro = IsTemaEscuro };
                Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
                File.WriteAllText(_configPath, JsonSerializer.Serialize(settings));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Erro ao salvar config: {ex.Message}"); }
        }

        private void ReiniciarAplicativo()
        {
            if (OperatingSystem.IsWindows())
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(processPath) { UseShellExecute = true });
            }

            if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
                lifetime.Shutdown();

            Environment.Exit(0);
        }

        // --- NOVA LÓGICA DE BACKUP COM STORAGE PROVIDER (ANDROID 14+ COMPATÍVEL) ---

        // Método auxiliar para pegar o provedor de arquivos nativo do Avalonia
        private IStorageProvider? GetStorageProvider()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow?.StorageProvider;

            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                return TopLevel.GetTopLevel(singleView.MainView)?.StorageProvider;

            return null;
        }

        [RelayCommand]
        private async Task BackupBancoDados()
        {
            try
            {
                var provider = GetStorageProvider();
                if (provider == null) return;

                // 1. Abre a tela nativa do sistema (Windows ou Android) pedindo onde salvar
                var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Salvar Backup do SmartCash",
                    SuggestedFileName = "SmartCashDbBackups.zip",
                    DefaultExtension = ".zip",
                    FileTypeChoices = new[] { new FilePickerFileType("Arquivo ZIP") { Patterns = new[] { "*.zip" } } }
                });

                if (file == null) return; // Usuário cancelou

                // 2. Prepara o ZIP em uma pasta temporária isolada do app
                string pastaSandbox = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string pastaTemp = Path.Combine(Path.GetTempPath(), "SmartCashBackupTemp_" + Guid.NewGuid().ToString());
                string arquivoZipTemp = Path.Combine(Path.GetTempPath(), "TempZip_" + Guid.NewGuid().ToString() + ".zip");

                Directory.CreateDirectory(pastaTemp);

                var arquivosNaSandbox = Directory.GetFiles(pastaSandbox);
                foreach (var arquivo in arquivosNaSandbox)
                {
                    if (arquivo.EndsWith(".db") || arquivo.EndsWith(".db-wal") || arquivo.EndsWith(".db-shm"))
                    {
                        File.Copy(arquivo, Path.Combine(pastaTemp, Path.GetFileName(arquivo)), true);
                    }
                }

                ZipFile.CreateFromDirectory(pastaTemp, arquivoZipTemp);

                // 3. Copia o ZIP temporário para o local seguro que o usuário escolheu via StorageProvider
                await using (var streamDestino = await file.OpenWriteAsync())
                {
                    await using (var streamOrigem = File.OpenRead(arquivoZipTemp))
                    {
                        await streamOrigem.CopyToAsync(streamDestino);
                    }
                }

                // 4. Limpeza
                Directory.Delete(pastaTemp, true);
                File.Delete(arquivoZipTemp);

                System.Diagnostics.Debug.WriteLine("Backup salvo com sucesso pelo usuário.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao fazer backup: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RestaurarBancoDados()
        {
            try
            {
                var provider = GetStorageProvider();
                if (provider == null) return;

                // 1. Abre a tela nativa do sistema para o usuário selecionar o ZIP de backup
                var result = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Selecione o Backup do SmartCash",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Arquivo ZIP") { Patterns = new[] { "*.zip" } } }
                });

                if (result.Count == 0) return; // Usuário cancelou

                var file = result[0];

                // 2. Lê o arquivo seguro e copia para a pasta temporária do app
                string arquivoZipTemp = Path.Combine(Path.GetTempPath(), "TempRestore_" + Guid.NewGuid().ToString() + ".zip");

                await using (var streamOrigem = await file.OpenReadAsync())
                {
                    await using (var streamDestino = File.Create(arquivoZipTemp))
                    {
                        await streamOrigem.CopyToAsync(streamDestino);
                    }
                }

                // 3. Extrai para a Sandbox
                string pastaSandbox = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                ZipFile.ExtractToDirectory(arquivoZipTemp, pastaSandbox, overwriteFiles: true);

                File.Delete(arquivoZipTemp);

                ReiniciarAplicativo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao restaurar: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ExportarBancoDados()
        {
            // Como agora o usuário escolhe onde salvar no Backup, 
            // o botão de Exportar talvez não seja mais necessário com a mesma lógica de antes,
            // pois o arquivo já estará numa pasta pública escolhida por ele.
            // Se desejar mantê-lo, a lógica precisará ser repensada.
            System.Diagnostics.Debug.WriteLine("Utilize o novo sistema de Backup para salvar onde desejar.");
        }
    }
}