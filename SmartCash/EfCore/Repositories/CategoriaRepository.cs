using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Repositories
{
    public class CategoriaRepository : IBaseRepository<CategoriaModel>
    {
        private readonly IDbContextFactory<MeuDbContext> _contextFactory;

        public CategoriaRepository(IDbContextFactory<MeuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<CategoriaModel>> GetAllAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                var query = db.Categorias.AsNoTracking();
                Debug.WriteLine($"Executando Query SQLite (Categorias): \n{query.ToQueryString()}");
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado em Categoria.GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<CategoriaModel?> GetByIdAsync(int id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                return await db.Categorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdCategoria == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar categoria {id}: {ex.Message}");
                return null;
            }
        }

        public async Task AddAsync(CategoriaModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                await db.Categorias.AddAsync(entity);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"Erro de persistência ao adicionar Categoria: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(CategoriaModel entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            try
            {
                var existente = await db.Categorias
                    .FirstOrDefaultAsync(x => x.IdCategoria == entity.IdCategoria);

                if (existente == null)
                {
                    throw new InvalidOperationException($"Categoria ID {entity.IdCategoria} não localizada para atualização.");
                }

                existente.Nome = entity.Nome;
                existente.IconeApresentacao = entity.IconeApresentacao;

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao atualizar categoria {entity.IdCategoria}: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            try
            {
                var registro = await db.Categorias.FirstOrDefaultAsync(x => x.IdCategoria == id);
                if (registro != null)
                {
                    db.Categorias.Remove(registro);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao deletar categoria {id}: {ex.Message}");
                throw;
            }
        }
    }
}