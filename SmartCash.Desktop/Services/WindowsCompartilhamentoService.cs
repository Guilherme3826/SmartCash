using SmartCash.Interfaces;
using System.Diagnostics;

namespace SmartCash.Desktop.Services
{
    public class WindowsCompartilhamentoService : ICompartilhamentoService
    {
        public void AbrirPastaDoArquivo(string caminhoArquivo)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{caminhoArquivo}\"",
                UseShellExecute = true
            });
        }

        public void CompartilharArquivo(string caminhoArquivo, string titulo)
        {
            // No Windows, estamos apenas abrindo a pasta conforme sua solicitação
            AbrirPastaDoArquivo(caminhoArquivo);
        }
    }
}