using Microsoft.EntityFrameworkCore;
using SmartCash.EfCore;
using SmartCash.EfCore.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartCash.EfCore.Repositories
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly MeuDbContext _context;

        protected BaseRepository(MeuDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<T>> ObterTodosAsync()
        {
            return await _context.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<T?> ObterPorIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task AdicionarAsync(T entidade)
        {
            await _context.Set<T>().AddAsync(entidade);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(T entidade)
        {
            _context.Set<T>().Update(entidade);
            await _context.SaveChangesAsync();
        }

        public async Task RemoverAsync(int id)
        {
            var entidade = await ObterPorIdAsync(id);
            if (entidade != null)
            {
                _context.Set<T>().Remove(entidade);
                await _context.SaveChangesAsync();
            }
        }
    }
}