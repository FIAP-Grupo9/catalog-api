using FCG.BuildingBlocks.Common;

namespace FCG.Catalog.Api.Domain.Entities;

// Projeção: "biblioteca do usuário". Escrita APENAS após PaymentProcessedEvent(Approved).
public class BibliotecaJogo : BaseEntity
{
    public Guid UsuarioId { get; set; }
    public Guid JogoId { get; set; }
    public Jogo? Jogo { get; set; }
    public DateTime DataCompra { get; set; }
    public decimal PrecoPago { get; set; }
    public Guid? PromocaoId { get; set; }
    public Promocao? Promocao { get; set; }
    public Guid OrderId { get; set; }
}
