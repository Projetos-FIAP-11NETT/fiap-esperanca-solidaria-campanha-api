namespace FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;

public enum CampaignStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3,

    // Adicionado depois, no fim, pra não renumerar os valores acima
    // (já existem linhas persistidas com esses inteiros).
    Scheduled = 4
}
