using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.EfCore.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<IEnumerable<T>> ObterTodosAsync();
        Task<T?> ObterPorIdAsync(int id);
        Task AdicionarAsync(T entidade);
        Task AtualizarAsync(T entidade);
        Task RemoverAsync(int id);
    }
}
