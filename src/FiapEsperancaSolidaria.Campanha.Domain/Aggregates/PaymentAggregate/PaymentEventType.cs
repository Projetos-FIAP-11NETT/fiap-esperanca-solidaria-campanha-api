namespace FiapEsperancaSolidaria.Campanha.Domain.Aggregates.PaymentAggregate;

public enum PaymentEventType : byte
{
    /// <summary>Curso normal: entrou em processamento, foi aprovado ou foi rejeitado.</summary>
    Info = 1,

    /// <summary>Excecao nao tratada — a doacao nao foi concluida e a mensagem sera reentregue.</summary>
    Critical = 2,

    /// <summary>Mensagem repetida: a doacao ja havia saido de <c>Pending</c>.</summary>
    Warning = 3,
}