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
    public class ItemRepository : IItemRepository
    {
        private readonly IDbContextFactory<MeuDbContext> _contextFactory;

        public ItemRepository(IDbContextFactory<MeuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<ItemModel>> GetAllAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                var query = db.Itens
                    .AsNoTracking()
                    .Include(i => i.Transacao)
                    .Include(i => i.Produto)
                        .ThenInclude(p => p.Categoria);

                Debug.WriteLine($"Executando Query SQLite (Itens com Produto e Transação): \n{query.ToQueryString()}");
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado em Item.GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ItemModel?> GetByIdAsync(int id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                return await db.Itens
                    .AsNoTracking()
                    .Include(i => i.Transacao)
                    .Include(i => i.Produto)
                        .ThenInclude(p => p.Categoria)
                    .FirstOrDefaultAsync(x => x.IdItem == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar item {id}: {ex.Message}");
                return null;
            }
        }

        public async Task AddAsync(ItemModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                await db.Itens.AddAsync(entity);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"Erro de persistência ao adicionar Item: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(ItemModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                var existente = await db.Itens
                    .FirstOrDefaultAsync(x => x.IdItem == entity.IdItem);

                if (existente == null)
                {
                    throw new InvalidOperationException($"Item ID {entity.IdItem} não localizado para atualização.");
                }

                existente.Quantidade = entity.Quantidade;
                existente.ValorUnit = entity.ValorUnit;
                existente.ValorTotal = entity.ValorTotal;
                existente.IdTransacao = entity.IdTransacao;
                existente.IdConsumivel = entity.IdConsumivel;

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao atualizar item {entity.IdItem}: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                var registro = await db.Itens.FirstOrDefaultAsync(x => x.IdItem == id);
                if (registro != null)
                {
                    db.Itens.Remove(registro);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao deletar item {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ItemModel>> GetItensPorMesAnoAsync(int mes, int ano)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.Itens
                .AsNoTracking()
                .Include(i => i.Transacao)
                .Include(i => i.Produto)
                    .ThenInclude(p => p.Categoria)
                .Where(i => i.Transacao.Data.Month == mes && i.Transacao.Data.Year == ano)
                .OrderByDescending(i => i.Transacao.Data)
                .ToListAsync();
        }
    }
}