using FiapEsperancaSolidaria.Campanha.Domain.Abstractions;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;

namespace FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;

public class Campaign : IAggregateRoot
{
    public Guid CampaignId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string? Image { get; private set; }
    public decimal FinancialGoal { get; private set; }
    public CampaignStatus Status { get; private set; }
    public decimal TotalRaised { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Donation> _donations = [];
    public IReadOnlyCollection<Donation> Donations => _donations.AsReadOnly();

    private Campaign()
    {
    }

    public static Campaign Create(
        string title,
        string description,
        DateTime startDate,
        DateTime endDate,
        decimal financialGoal,
        string? image = null)
    {
        var campaign = new Campaign
        {
            CampaignId = Guid.NewGuid(),
            TotalRaised = 0,
            Status = startDate.Date <= DateTime.UtcNow.Date ? CampaignStatus.Active : CampaignStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        campaign.Update(title, description, startDate, endDate, financialGoal, image);

        return campaign;
    }

    public void Update(
        string title,
        string description,
        DateTime startDate,
        DateTime endDate,
        decimal financialGoal,
        string? image = null)
    {
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateEndDate(endDate);
        ValidateFinancialGoal(financialGoal);

        Title = title;
        Description = description;
        StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        FinancialGoal = financialGoal;
        Image = image;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImage(string imageKey)
    {
        Image = imageKey;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status != CampaignStatus.Scheduled)
            throw new BusinessException("Só uma campanha programada pode ser ativada.");

        Status = CampaignStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status == CampaignStatus.Cancelled)
            throw new BusinessException("Não é possível concluir uma campanha cancelada.");

        Status = CampaignStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == CampaignStatus.Completed)
            throw new BusinessException("Não é possível cancelar uma campanha já concluída.");

        // Se a meta já foi atingida quando pedem o cancelamento, não é uma
        // campanha cancelada de fato — é uma campanha concluída com sucesso.
        // TotalRaised é a fonte de verdade (mantida pelo doacao-work a partir do
        // evento de doação aprovada); some as Donations locais só como fallback
        // pra quando TotalRaised ainda não foi atualizado (ex.: doacao-work não
        // está rodando neste ambiente).
        var raisedAmount = TotalRaised > 0 ? TotalRaised : _donations.Sum(d => d.Amount);

        if (raisedAmount >= FinancialGoal)
        {
            Complete();
            return;
        }

        Status = CampaignStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private bool CanReceiveDonation() => Status == CampaignStatus.Active;

    public Donation AddDonation(Guid donorId, decimal amount, PaymentMethod paymentMethod)
    {
        if (!CanReceiveDonation())
            throw new BusinessException("Não é possível registrar uma doação para uma campanha que não está ativa.");

        var donation = new Donation(CampaignId, donorId, amount, paymentMethod);
        _donations.Add(donation);

        return donation;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("O título da campanha é obrigatório.");
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("A descrição da campanha é obrigatória.");
    }

    private static void ValidateEndDate(DateTime endDate)
    {
        if (endDate.Date < DateTime.UtcNow.Date)
            throw new DomainException("A data de término da campanha não pode estar no passado.");
    }

    private static void ValidateFinancialGoal(decimal financialGoal)
    {
        if (financialGoal <= 0)
            throw new DomainException("A meta financeira deve ser maior que zero.");
    }
}
