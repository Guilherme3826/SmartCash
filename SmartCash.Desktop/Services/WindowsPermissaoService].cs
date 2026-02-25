using SmartCash.Interfaces;
using System.Threading.Tasks;

namespace SmartCash.Desktop.Services
{
    public class WindowsPermissaoService : IPermissaoService
    {
        public Task<bool> SolicitarPermissaoArmazenamentoAsync()
        {
            // Windows (Desktop) geralmente tem acesso à pasta do usuário sem popup
            return Task.FromResult(true);
        }
    }
}