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

        // Mapeamento padrão do EF Core para enum: inteiro (Ativa=1, Concluida=2, Cancelada=3).
        builder.Property(c => c.Status);

        builder.HasIndex(c => c.Status);

        // Coluna gerada com o título em minúsculas, só pra sustentar o índice único
        // case-insensitive abaixo — não é exposta na entidade de domínio.
        builder.Property<string>("TituloNormalizado")
            .HasComputedColumnSql("lower(\"Titulo\")", stored: true);

        builder.HasIndex("TituloNormalizado")
            .IsUnique()
            .HasDatabaseName("IX_Campanhas_TituloNormalizado");
    }
}
