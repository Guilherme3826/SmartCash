namespace SmartCash.Interfaces
{
    public interface ICompartilhamentoService
    {
        void CompartilharArquivo(string caminho, string titulo);
        void AbrirPastaDoArquivo(string caminhoArquivo);
    }
}