using FiapEsperancaSolidaria.Campanha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Data.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Domain.Entities.Campaign>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Campaign> builder)
    {
        builder.ToTable("Campaigns");

        builder.HasKey(c => c.CampaignId);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .IsRequired();

        builder.Property(c => c.Image)
            .HasMaxLength(500);

        builder.Property(c => c.FinancialGoal)
            .HasPrecision(18, 2);

        builder.Property(c => c.TotalRaised)
            .HasPrecision(18, 2);

        // Mapeamento padrão do EF Core para enum: inteiro (Active=1, Completed=2, Cancelled=3).
        builder.Property(c => c.Status);

        builder.HasIndex(c => c.Status);

        // Coluna gerada com o título em minúsculas, só pra sustentar o índice único
        // case-insensitive abaixo — não é exposta na entidade de domínio.
        builder.Property<string>("NormalizedTitle")
            .HasComputedColumnSql("lower(\"Title\")", stored: true);

        builder.HasIndex("NormalizedTitle")
            .IsUnique()
            .HasDatabaseName("IX_Campaigns_NormalizedTitle");

        // Campaign é a raiz do agregado: Donation só existe atrelada a uma campanha
        // (sem navigation property de volta — Donation não referencia Campaign diretamente).
        builder.HasMany(c => c.Donations)
            .WithOne()
            .HasForeignKey(d => d.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
