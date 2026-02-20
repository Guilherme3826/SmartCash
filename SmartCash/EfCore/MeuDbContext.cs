using Microsoft.EntityFrameworkCore;
using SmartCash.EfCore.Models;
using System.Reflection;

namespace SmartCash.EfCore
{
    public class MeuDbContext : DbContext
    {
        // Construtor obrigatório para receber as configurações da DI
        public MeuDbContext(DbContextOptions<MeuDbContext> options) : base(options)
        {
        }

        public DbSet<CategoriaModel> Categorias { get; set; }
        public DbSet<ProdutoModel> Produtos { get; set; }
        public DbSet<TransacaoModel> Transacoes { get; set; }
        public DbSet<ItemModel> Itens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        // OnConfiguring pode ficar vazio ou ser removido se você só usar via DI
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}