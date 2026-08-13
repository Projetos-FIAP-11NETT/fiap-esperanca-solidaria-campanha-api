using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;

namespace FiapEsperancaSolidaria.Campanha.Domain.Entities;

public class Campanha
{
    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public DateTime DataInicio { get; private set; }
    public DateTime DataFim { get; private set; }
    public string? Imagem { get; private set; }
    public decimal MetaFinanceira { get; private set; }
    public StatusCampanha Status { get; private set; }
    public decimal ValorTotalArrecadado { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Campanha()
    {
    }

    public static Campanha Criar(
        string titulo,
        string descricao,
        DateTime dataInicio,
        DateTime dataFim,
        decimal metaFinanceira,
        string? imagem = null)
    {
        var campanha = new Campanha
        {
            Id = Guid.NewGuid(),
            ValorTotalArrecadado = 0,
            Status = StatusCampanha.Ativa,
            CriadoEm = DateTime.UtcNow
        };

        campanha.AtualizarDados(titulo, descricao, dataInicio, dataFim, metaFinanceira, imagem);

        return campanha;
    }

    public void AtualizarDados(
        string titulo,
        string descricao,
        DateTime dataInicio,
        DateTime dataFim,
        decimal metaFinanceira,
        string? imagem = null)
    {
        ValidarTitulo(titulo);
        ValidarDescricao(descricao);
        ValidarDataFim(dataFim);
        ValidarMetaFinanceira(metaFinanceira);

        Titulo = titulo;
        Descricao = descricao;
        DataInicio = dataInicio;
        DataFim = dataFim;
        MetaFinanceira = metaFinanceira;
        Imagem = imagem;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void DefinirImagem(string chaveImagem)
    {
        Imagem = chaveImagem;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Concluir()
    {
        if (Status == StatusCampanha.Cancelada)
            throw new BusinessException("Não é possível concluir uma campanha cancelada.");

        Status = StatusCampanha.Concluida;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        if (Status == StatusCampanha.Concluida)
            throw new BusinessException("Não é possível cancelar uma campanha já concluída.");

        Status = StatusCampanha.Cancelada;
        AtualizadoEm = DateTime.UtcNow;
    }

    public bool PodeReceberDoacao() => Status == StatusCampanha.Ativa;

    public void AlterarStatus(StatusCampanha novoStatus)
    {
        switch (novoStatus)
        {
            case StatusCampanha.Cancelada:
                Cancelar();
                break;
            case StatusCampanha.Concluida:
                Concluir();
                break;
            case StatusCampanha.Ativa:
                if (Status == StatusCampanha.Concluida)
                    throw new BusinessException("Não é possível reativar uma campanha já concluída.");

                Status = StatusCampanha.Ativa;
                AtualizadoEm = DateTime.UtcNow;
                break;
        }
    }

    private static void ValidarTitulo(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título da campanha é obrigatório.");
    }

    private static void ValidarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("A descrição da campanha é obrigatória.");
    }

    private static void ValidarDataFim(DateTime dataFim)
    {
        if (dataFim.Date < DateTime.UtcNow.Date)
            throw new DomainException("A data de término da campanha não pode estar no passado.");
    }

    private static void ValidarMetaFinanceira(decimal metaFinanceira)
    {
        if (metaFinanceira <= 0)
            throw new DomainException("A meta financeira deve ser maior que zero.");
    }
}
