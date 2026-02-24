using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartCash.EfCore.Configurations;
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
        public DbSet<ConsumiveisModel> Consumivel { get; set; }
        public DbSet<TransacaoModel> Transacoes { get; set; }
        public DbSet<ItemModel> Itens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new CategoriaConfiguration());
            modelBuilder.ApplyConfiguration(new ItemConfiguration());
            modelBuilder.ApplyConfiguration(new ConsumivelConfiguration());
            modelBuilder.ApplyConfiguration(new TransacaoConfiguration());
            base.OnModelCreating(modelBuilder);
        }

        // OnConfiguring pode ficar vazio ou ser removido se você só usar via DI
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }

    /// <summary>
    /// Fábrica usada pelas ferramentas do EF Core (Console do Gerenciador de Pacotes) 
    /// para criar o DbContext em tempo de design no Windows.
    /// </summary>
    public class MeuDbContextFactory : IDesignTimeDbContextFactory<MeuDbContext>
    {
        public MeuDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MeuDbContext>();

            // Define o provedor SQLite apenas para a geração do esquema (migrações).
            // Esse arquivo temporário não afetará o banco de dados real do Android.
            optionsBuilder.UseSqlite("Data Source=design_time_temp.db");

            return new MeuDbContext(optionsBuilder.Options);
        }
    }
}