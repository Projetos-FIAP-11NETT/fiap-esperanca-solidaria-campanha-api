using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.PaymentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Data.Configurations;

public class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> builder)
    {
        builder.ToTable(nameof(PaymentEvent));

        builder.HasKey(g => g.PaymentEventId);

        builder.Property(g => g.PaymentEventId)
            .IsRequired();

        builder.Property(g => g.DonationId)
            .IsRequired();

        builder.Property(g => g.PaymentEventType)
            .IsRequired();

        builder.Property(g => g.Observation)
            .HasMaxLength(255);

        builder.Property(g => g.CreateAt)
            .IsRequired();
    }
}