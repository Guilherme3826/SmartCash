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
    public class TransacaoRepository : IBaseRepository<TransacaoModel>
    {
        private readonly IDbContextFactory<MeuDbContext> _contextFactory;

        public TransacaoRepository(IDbContextFactory<MeuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<TransacaoModel>> GetAllAsync(Func<IQueryable<TransacaoModel>, IQueryable<TransacaoModel>>? include = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Query única trazendo toda a árvore de dados: Transação -> Itens -> Produto
                var query = db.Transacoes
                    .AsNoTracking()
                    .Include(t => t.Itens)
                        .ThenInclude(i => i.Produto)
                    .AsQueryable();

                if (include != null)
                {
                    query = include(query);
                }

                Debug.WriteLine($"Executando Query SQLite (Transações Completas): \n{query.ToQueryString()}");

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado em Transacao.GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<TransacaoModel?> GetByIdAsync(int id, Func<IQueryable<TransacaoModel>, IQueryable<TransacaoModel>>? include = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                var query = db.Transacoes
                    .AsNoTracking()
                    .Include(t => t.Itens)
                        .ThenInclude(i => i.Produto)
                    .AsQueryable();

                if (include != null)
                {
                    query = include(query);
                }

                return await query.FirstOrDefaultAsync(x => x.IdTransacao == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar transação {id}: {ex.Message}");
                return null;
            }
        }

        public async Task AddAsync(TransacaoModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                await db.Transacoes.AddAsync(entity);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"Erro de persistência ao adicionar Transação: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(TransacaoModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Busca o registro para garantir o rastreamento individual e atualização campo a campo
                var existente = await db.Transacoes
                    .FirstOrDefaultAsync(x => x.IdTransacao == entity.IdTransacao);

                if (existente == null)
                {
                    throw new InvalidOperationException($"Transação ID {entity.IdTransacao} não localizada.");
                }

                // Atualização individual das propriedades de dados
                existente.Data = entity.Data;
                existente.ValorTotal = entity.ValorTotal;

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao atualizar transação {entity.IdTransacao}: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                var registro = await db.Transacoes.FirstOrDefaultAsync(x => x.IdTransacao == id);
                if (registro != null)
                {
                    db.Transacoes.Remove(registro);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao deletar transação {id}: {ex.Message}");
                throw;
            }
        }
    }
}