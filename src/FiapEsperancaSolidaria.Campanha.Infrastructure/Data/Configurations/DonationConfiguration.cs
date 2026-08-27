using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.DonationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Data.Configurations;

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable(nameof(Donation));

        builder.HasKey(g => g.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd();

        builder.Property(g => g.CampaignId)
            .IsRequired();

        builder.Property(g => g.DonorId)
            .IsRequired();

        builder.Property(g => g.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.PaymentMethod)
            .IsRequired();

        builder.Property(o => o.DonationStatus)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();
    }
}