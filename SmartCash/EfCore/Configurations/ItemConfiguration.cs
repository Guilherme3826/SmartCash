using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Configurations
{
    public class ItemConfiguration : IEntityTypeConfiguration<ItemModel>
    {
        public void Configure(EntityTypeBuilder<ItemModel> builder)
        {
            builder.ToTable("Item", t =>
            {
                t.HasCheckConstraint("CHK_Item_Quantidade", "Quantidade >= 0");
                t.HasCheckConstraint("CHK_Item_ValorUnit", "ValorUnit >= 0");
                t.HasCheckConstraint("CHK_Item_ValorTotal", "ValorTotal >= 0");
            });

            builder.HasKey(e => e.IdItem);
            builder.Property(e => e.Quantidade).IsRequired();
            builder.Property(e => e.ValorUnit).IsRequired();
            builder.Property(e => e.ValorTotal).IsRequired();

            builder.HasOne(d => d.Transacao)
                .WithMany(p => p.Itens)
                .HasForeignKey(d => d.IdTransacao)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Produto)
                .WithMany(p => p.Itens)
                .HasForeignKey(d => d.IdProduto)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}