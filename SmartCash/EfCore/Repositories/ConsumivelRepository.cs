using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Repositories
{
    public class ConsumivelRepository : IBaseRepository<ConsumiveisModel>
    {
        private readonly IDbContextFactory<MeuDbContext> _contextFactory;

        public ConsumivelRepository(IDbContextFactory<MeuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<ConsumiveisModel>> GetAllAsync(Func<IQueryable<ConsumiveisModel>, IQueryable<ConsumiveisModel>>? include = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Includes inseridos diretamente no repositório conforme solicitado
                var query = db.Consumivel
                    .AsNoTracking()
                    .Include(p => p.Categoria)              
                    .AsQueryable();

                // Mantém suporte a includes adicionais via parâmetro para respeitar a IBaseRepository
                if (include != null)
                {
                    query = include(query);
                }

                Debug.WriteLine($"Executando Query SQLite (Produtos com Includes): \n{query.ToQueryString()}");

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado em Produto.GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ConsumiveisModel?> GetByIdAsync(int id, Func<IQueryable<ConsumiveisModel>, IQueryable<ConsumiveisModel>>? include = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Includes inseridos diretamente para garantir dados completos na busca por ID
                var query = db.Consumivel
                    .AsNoTracking()
                    .Include(p => p.Categoria)                 
                    .AsQueryable();

                if (include != null)
                {
                    query = include(query);
                }

                return await query.FirstOrDefaultAsync(x => x.IdConsumivel == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar produto {id}: {ex.Message}");
                return null;
            }
        }

        public async Task AddAsync(ConsumiveisModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                await db.Consumivel.AddAsync(entity);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"Erro de persistência ao adicionar Produto: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(ConsumiveisModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Busca o registro no contexto atual para garantir o rastreamento e atualização individual
                var existente = await db.Consumivel
                    .FirstOrDefaultAsync(x => x.IdConsumivel == entity.IdConsumivel);

                if (existente == null)
                {
                    throw new InvalidOperationException($"Produto ID {entity.IdConsumivel} não localizado para atualização.");
                }

                // Atualização individual de cada campo para evitar problemas com navegação
                existente.Nome = entity.Nome;
                existente.IdCategoria = entity.IdCategoria;
                existente.Valor = entity.Valor;

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao atualizar produto {entity.IdConsumivel}: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                var registro = await db.Consumivel.FirstOrDefaultAsync(x => x.IdConsumivel == id);
                if (registro != null)
                {
                    db.Consumivel.Remove(registro);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao deletar produto {id}: {ex.Message}");
                throw;
            }
        }
    }
}