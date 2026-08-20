namespace FiapEsperancaSolidaria.Campanha.Domain.Aggregates.DonationAggregate;

public enum PaymentMethod : byte
{
    CreditCard  = 1,
    DebitCard = 2,
    Pix = 3,
    Boleto = 4
}