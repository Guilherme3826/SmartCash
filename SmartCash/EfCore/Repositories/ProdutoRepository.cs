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
    public class ProdutoRepository : IBaseRepository<ProdutoModel>
    {
        private readonly IDbContextFactory<MeuDbContext> _contextFactory;

        public ProdutoRepository(IDbContextFactory<MeuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<ProdutoModel>> GetAllAsync(Func<IQueryable<ProdutoModel>, IQueryable<ProdutoModel>>? include = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Includes inseridos diretamente no repositório conforme solicitado
                var query = db.Produtos
                    .AsNoTracking()
                    .Include(p => p.Categoria)
                    .Include(p => p.Itens)
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

        public async Task<ProdutoModel?> GetByIdAsync(int id, Func<IQueryable<ProdutoModel>, IQueryable<ProdutoModel>>? include = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Includes inseridos diretamente para garantir dados completos na busca por ID
                var query = db.Produtos
                    .AsNoTracking()
                    .Include(p => p.Categoria)
                    .Include(p => p.Itens)
                    .AsQueryable();

                if (include != null)
                {
                    query = include(query);
                }

                return await query.FirstOrDefaultAsync(x => x.IdProduto == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar produto {id}: {ex.Message}");
                return null;
            }
        }

        public async Task AddAsync(ProdutoModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                await db.Produtos.AddAsync(entity);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"Erro de persistência ao adicionar Produto: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(ProdutoModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                // Busca o registro no contexto atual para garantir o rastreamento e atualização individual
                var existente = await db.Produtos
                    .FirstOrDefaultAsync(x => x.IdProduto == entity.IdProduto);

                if (existente == null)
                {
                    throw new InvalidOperationException($"Produto ID {entity.IdProduto} não localizado para atualização.");
                }

                // Atualização individual de cada campo para evitar problemas com navegação
                existente.Nome = entity.Nome;
                existente.IdCategoria = entity.IdCategoria;
                existente.Valor = entity.Valor;

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao atualizar produto {entity.IdProduto}: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                var registro = await db.Produtos.FirstOrDefaultAsync(x => x.IdProduto == id);
                if (registro != null)
                {
                    db.Produtos.Remove(registro);
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