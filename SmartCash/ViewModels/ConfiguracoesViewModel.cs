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

        [ObservableProperty] private string? _bookmarkPastaBackup;
        [ObservableProperty] private string? _nomePastaBackup;

        private readonly ICompartilhamentoService? _compartilhamentoService;

        public ConfiguracoesViewModel()
        {
            _compartilhamentoService = App.ServiceProvider.GetService<ICompartilhamentoService>();

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
                        AmbienteSelecionado = settings.Ambiente ?? "Produção";
                        IsTemaEscuro = settings.ModoEscuro;
                        BookmarkPastaBackup = settings.BookmarkPastaBackup;
                        NomePastaBackup = settings.NomePastaBackup;

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
                    ModoEscuro = IsTemaEscuro,
                    BookmarkPastaBackup = BookmarkPastaBackup,
                    NomePastaBackup = NomePastaBackup
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

            if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }

            Environment.Exit(0);
        }

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

                // Gera o sufixo de data e hora no formato: 2026-02-25_19-05
                // Este formato é seguro para Windows, Android e organiza bem por nome.
                string dataHoraSegura = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
                string nomeSugerido = $"SmartCash_Backup_{dataHoraSegura}";

                var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Salvar Backup do SmartCash",
                    SuggestedFileName = nomeSugerido,
                    DefaultExtension = ".zip",
                    FileTypeChoices = new[] { new FilePickerFileType("Arquivo ZIP") { Patterns = new[] { "*.zip" } } }
                });

                if (file == null) return;

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

                await using (var streamDestino = await file.OpenWriteAsync())
                {
                    await using (var streamOrigem = File.OpenRead(arquivoZipTemp))
                    {
                        await streamOrigem.CopyToAsync(streamDestino);
                    }
                }

                Directory.Delete(pastaTemp, true);
                File.Delete(arquivoZipTemp);

                System.Diagnostics.Debug.WriteLine($"[Backup] Sucesso: {nomeSugerido}");
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

                var result = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Selecione o Backup do SmartCash",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Arquivo ZIP") { Patterns = new[] { "*.zip" } } }
                });

                if (result.Count == 0) return;

                var file = result[0];
                string arquivoZipTemp = Path.Combine(Path.GetTempPath(), "TempRestore_" + Guid.NewGuid().ToString() + ".zip");

                await using (var streamOrigem = await file.OpenReadAsync())
                {
                    await using (var streamDestino = File.Create(arquivoZipTemp))
                    {
                        await streamOrigem.CopyToAsync(streamDestino);
                    }
                }

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
        private async Task ExportarBancoDados()
        {
            System.Diagnostics.Debug.WriteLine("[Exportar] Iniciando método ExportarBancoDados...");

            try
            {
                if (_compartilhamentoService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Exportar] ERRO: _compartilhamentoService está nulo. A injeção de dependência falhou.");
                    return;
                }
                System.Diagnostics.Debug.WriteLine("[Exportar] Serviço de compartilhamento validado com sucesso.");

                if (string.IsNullOrEmpty(BookmarkPastaBackup))
                {
                    System.Diagnostics.Debug.WriteLine("[Exportar] BLOQUEADO: BookmarkPastaBackup está nulo ou vazio. O usuário não definiu a pasta de backup.");
                    // Pode adicionar uma notificação ao utilizador aqui
                    return;
                }
                System.Diagnostics.Debug.WriteLine("[Exportar] Bookmark verificado. Iniciando geração do backup silencioso...");

                // Efetua o backup e solicita a preservação do ficheiro temporário para o partilhar
                string? caminhoArquivoParaCompartilhar = await ExecutarAutoBackupSilencioso(manterArquivoTemp: true);

                System.Diagnostics.Debug.WriteLine($"[Exportar] Retorno do ExecutarAutoBackupSilencioso: '{caminhoArquivoParaCompartilhar}'");

                if (string.IsNullOrEmpty(caminhoArquivoParaCompartilhar))
                {
                    System.Diagnostics.Debug.WriteLine("[Exportar] FALHA: O caminho do arquivo retornado é nulo ou vazio. O processo de backup falhou internamente.");
                    return;
                }

                bool arquivoExiste = File.Exists(caminhoArquivoParaCompartilhar);
                System.Diagnostics.Debug.WriteLine($"[Exportar] Verificação no disco: O arquivo realmente existe? {arquivoExiste}");

                if (arquivoExiste)
                {
                    if (OperatingSystem.IsAndroid())
                    {
                        System.Diagnostics.Debug.WriteLine("[Exportar] SO Detectado: Android. Chamando a interface de compartilhamento nativa...");
                        _compartilhamentoService.CompartilharArquivo(caminhoArquivoParaCompartilhar, "Backup SmartCash");
                        System.Diagnostics.Debug.WriteLine("[Exportar] Comando de compartilhamento Android enviado com sucesso.");
                    }
                    else if (OperatingSystem.IsWindows())
                    {
                        System.Diagnostics.Debug.WriteLine("[Exportar] SO Detectado: Windows. Chamando abertura de pasta nativa...");
                        _compartilhamentoService.AbrirPastaDoArquivo(caminhoArquivoParaCompartilhar);
                        System.Diagnostics.Debug.WriteLine("[Exportar] Comando de abertura de pasta Windows enviado com sucesso.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[Exportar] SO Detectado: Não suportado pela lógica atual.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Exportar] FALHA: O método de backup retornou um caminho, mas o File.Exists diz que o arquivo não está lá.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Exportar] EXCEÇÃO CRÍTICA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Exportar] StackTrace: {ex.StackTrace}");
            }
        }

        [RelayCommand]
        private async Task DefinirPastaAutoBackup()
        {
            try
            {
                var provider = GetStorageProvider();
                if (provider == null) return;

                var pastas = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Escolha a pasta para Backups Automáticos",
                    AllowMultiple = false
                });

                if (pastas.Count == 0) return;

                var pastaSelecionada = pastas[0];
                var bookmark = await pastaSelecionada.SaveBookmarkAsync();

                if (!string.IsNullOrEmpty(bookmark))
                {
                    BookmarkPastaBackup = bookmark;
                    NomePastaBackup = pastaSelecionada.Name;

                    SalvarConfiguracoesNoArquivo();
                    System.Diagnostics.Debug.WriteLine("Pasta configurada com sucesso para auto-backup!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao configurar pasta auto-backup: {ex.Message}");
            }
        }

        public async Task<string?> ExecutarAutoBackupSilencioso(bool manterArquivoTemp = false)
        {
            try
            {
                string pastaSandbox = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string idUnico = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string pastaTemp = Path.Combine(Path.GetTempPath(), "SmartCashTemp_" + idUnico);
                string arquivoZipTemp = Path.Combine(Path.GetTempPath(), $"SmartCash_AutoBackup_{idUnico}.zip");

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

                Directory.Delete(pastaTemp, true);

                return arquivoZipTemp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro no auto-backup silencioso: {ex.Message}");
                return null;
            }
        }
    }
}