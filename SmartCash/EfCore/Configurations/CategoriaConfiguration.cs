using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Configurations
{
    public class CategoriaConfiguration : IEntityTypeConfiguration<CategoriaModel>
    {
        public void Configure(EntityTypeBuilder<CategoriaModel> builder)
        {
            builder.ToTable("Categoria");
            builder.HasKey(e => e.IdCategoria);
            builder.Property(e => e.Nome).IsRequired().HasMaxLength(50);
        }
    }
}