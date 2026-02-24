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
                // CORREÇÃO AOT: Consultas planas e simples que o Source Generator compila facilmente.
                // Carregamos as transações primeiro (o EF Core começa a rastreá-las).
                var transacoes = await db.Transacoes.ToListAsync();

                var ids = transacoes.Select(t => t.IdTransacao).ToList();

                if (ids.Any())
                {
                    // Carregamos os itens e seus produtos vinculados a essas transações.
                    // O recurso de "Navigation Fix-up" do EF Core vai automaticamente injetar 
                    // esses itens dentro da propriedade 'Itens' de cada 'TransacaoModel' carregada acima.
                    await db.Itens
                        .Include(i => i.Produto)
                        .Where(i => ids.Contains(i.IdTransacao))
                        .ToListAsync();
                }

                return transacoes;
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
                // AOT SAFE: Abordagem idêntica para evitar falhas no detalhamento.
                var transacao = await db.Transacoes.FirstOrDefaultAsync(x => x.IdTransacao == id);

                if (transacao != null)
                {
                    await db.Itens
                        .Include(i => i.Produto)
                            .ThenInclude(p => p.Categoria)
                        .Where(i => i.IdTransacao == id)
                        .ToListAsync();
                }

                return transacao;
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
            try
            {
                // Utiliza a rota AOT Safe que construímos acima
                var transacoes = await GetAllAsync();

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
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao gerar histórico mensal: {ex.Message}");
                return new List<ResumoMesModel>();
            }
        }
    }
}