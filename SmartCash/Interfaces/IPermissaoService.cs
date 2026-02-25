using System.Threading.Tasks;

namespace SmartCash.Interfaces
{
    public interface IPermissaoService
    {
        Task<bool> SolicitarPermissaoArmazenamentoAsync();
    }
}