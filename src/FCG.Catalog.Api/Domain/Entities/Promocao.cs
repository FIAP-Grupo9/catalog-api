using FCG.BuildingBlocks.Common;
using FCG.Catalog.Api.Domain.Enums;

namespace FCG.Catalog.Api.Domain.Entities;

public class Promocao : BaseEntity
{
    public string? Nome { get; set; }
    public TipoDesconto TipoDesconto { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public bool Ativa { get; set; } = true;
    public Guid IdJogo { get; set; }
    public Jogo? Jogo { get; set; }
}
