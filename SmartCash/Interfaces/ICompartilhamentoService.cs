namespace SmartCash.Interfaces
{
    public interface ICompartilhamentoService
    {
        void CompartilharArquivo(string caminhoArquivo, string titulo);
        void AbrirPastaDoArquivo(string caminhoArquivo);
    }
}