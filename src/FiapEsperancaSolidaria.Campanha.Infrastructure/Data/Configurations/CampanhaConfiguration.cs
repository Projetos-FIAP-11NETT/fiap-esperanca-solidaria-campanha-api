using FiapEsperancaSolidaria.Campanha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Data.Configurations;

public class CampanhaConfiguration : IEntityTypeConfiguration<Domain.Entities.Campanha>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Campanha> builder)
    {
        builder.ToTable("Campanhas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Titulo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Descricao)
            .IsRequired();

        builder.Property(c => c.Imagem)
            .HasMaxLength(500);

        builder.Property(c => c.MetaFinanceira)
            .HasPrecision(18, 2);

        builder.Property(c => c.ValorTotalArrecadado)
            .HasPrecision(18, 2);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Status);
    }
}
