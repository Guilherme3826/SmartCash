using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.EfCore.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        // O parâmetro 'include' permite injetar os .Include() de cada implementação
        Task<List<T>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? include = null);

        // Agora o GetById também aceita a lógica de carregamento de relações
        Task<T?> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? include = null);

        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
}
