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
    public class TransacaoRepository : ITransacaoRepository
    {
        private readonly IDbContextFactory<MeuDbContext> _contextFactory;

        public TransacaoRepository(IDbContextFactory<MeuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<TransacaoModel>> GetAllAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                var query = db.Transacoes
                    .AsNoTracking()
                    .Include(t => t.Itens)
                        .ThenInclude(i => i.Produto);

                Debug.WriteLine($"Executando Query SQLite (Transações Completas): \n{query.ToQueryString()}");
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado em Transacao.GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<TransacaoModel?> GetByIdAsync(int id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                return await db.Transacoes
                    .AsNoTracking()
                    .Include(t => t.Itens)
                        .ThenInclude(i => i.Produto)
                            .ThenInclude(i => i.Categoria)
                    .FirstOrDefaultAsync(x => x.IdTransacao == id);
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
                var existente = await db.Transacoes
                    .FirstOrDefaultAsync(x => x.IdTransacao == entity.IdTransacao);

                if (existente == null)
                {
                    throw new InvalidOperationException($"Transação ID {entity.IdTransacao} não localizada.");
                }

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

        public async Task<List<ResumoMesModel>> GetHistoricoMensalAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var transacoes = await db.Transacoes.AsNoTracking().ToListAsync();

            var historico = transacoes
                .GroupBy(t => new { t.Data.Year, t.Data.Month })
                .Select(g => new ResumoMesModel
                {
                    Ano = g.Key.Year,
                    Mes = g.Key.Month,
                    MesAnoApresentacao = $"{g.Key.Month:D2}/{g.Key.Year}",
                    Total = g.Sum(x => x.ValorTotal)
                })
                .OrderByDescending(x => x.Ano)
                .ThenByDescending(x => x.Mes)
                .ToList();

            return historico;
        }
    }
}