namespace FCG.Catalog.Api.Domain.Entities;

// Idempotência camada 1: registra MessageIds já processados por consumer.
public class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
