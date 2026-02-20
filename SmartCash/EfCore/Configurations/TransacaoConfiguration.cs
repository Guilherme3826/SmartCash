using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Configurations
{
    public class TransacaoConfiguration : IEntityTypeConfiguration<TransacaoModel>
    {
        public void Configure(EntityTypeBuilder<TransacaoModel> builder)
        {
            builder.ToTable("Transacao", t =>
            {
                t.HasCheckConstraint("CHK_Transacao_ValorTotal", "ValorTotal >= 0");
            });

            builder.HasKey(e => e.IdTransacao);
            builder.Property(e => e.Data).IsRequired();
            builder.Property(e => e.ValorTotal).IsRequired();
        }
    }
}