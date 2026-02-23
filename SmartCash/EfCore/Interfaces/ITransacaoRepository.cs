using SmartCash.EfCore.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartCash.EfCore.Interfaces
{
    public interface ITransacaoRepository : IBaseRepository<TransacaoModel>
    {
        Task<List<ResumoMesModel>> GetHistoricoMensalAsync();
    }
}