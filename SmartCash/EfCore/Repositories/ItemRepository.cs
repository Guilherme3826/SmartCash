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
    public class ItemRepository : IBaseRepository<ItemModel>
    {
        private readonly IDbContextFactory<MeuDbContext> _contextFactory;

        public ItemRepository(IDbContextFactory<MeuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<ItemModel>> GetAllAsync(Func<IQueryable<ItemModel>, IQueryable<ItemModel>>? include = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Query única com Includes fixos diretamente no repositório
                var query = db.Itens
                    .AsNoTracking()
                    .Include(i => i.Transacao)
                    .Include(i => i.Produto)
                        .ThenInclude(p => p.Categoria)
                    .AsQueryable();

                // Aplica Includes adicionais se fornecidos via parâmetro
                if (include != null)
                {
                    query = include(query);
                }

                Debug.WriteLine($"Executando Query SQLite (Itens com Produto e Transação): \n{query.ToQueryString()}");

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado em Item.GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ItemModel?> GetByIdAsync(int id, Func<IQueryable<ItemModel>, IQueryable<ItemModel>>? include = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                var query = db.Itens
                    .AsNoTracking()
                    .Include(i => i.Transacao)
                    .Include(i => i.Produto)
                        .ThenInclude(p => p.Categoria)
                    .AsQueryable();

                if (include != null)
                {
                    query = include(query);
                }

                // Busca utilizando a PK específica: IdItem
                return await query.FirstOrDefaultAsync(x => x.IdItem == id);
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
                // Busca o registro no contexto atual para garantir o rastreamento e atualização individual
                var existente = await db.Itens
                    .FirstOrDefaultAsync(x => x.IdItem == entity.IdItem);

                if (existente == null)
                {
                    throw new InvalidOperationException($"Item ID {entity.IdItem} não localizado para atualização.");
                }

                // Atualização individual de cada campo conforme solicitado
                existente.Quantidade = entity.Quantidade;
                existente.ValorUnit = entity.ValorUnit;
                existente.ValorTotal = entity.ValorTotal;
                existente.IdTransacao = entity.IdTransacao;
                existente.IdProduto = entity.IdProduto;

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
    }
}