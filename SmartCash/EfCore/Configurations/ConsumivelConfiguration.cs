using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Configurations
{
    public class ConsumivelConfiguration : IEntityTypeConfiguration<ConsumiveisModel>
    {
        public void Configure(EntityTypeBuilder<ConsumiveisModel> builder)
        {
            builder.ToTable("Consumivel", t =>
            {
                t.HasCheckConstraint("CHK_Produto_Valor", "Valor >= 0");
            });

            builder.HasKey(e => e.IdConsumivel);
            builder.Property(e => e.Nome).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Valor).IsRequired();

            builder.HasOne(d => d.Categoria)
                .WithMany(p => p.Produtos)
                .HasForeignKey(d => d.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}