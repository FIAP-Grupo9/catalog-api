using FCG.BuildingBlocks.Common;

namespace FCG.Catalog.Api.Domain.Entities;

public class Jogo : BaseEntity
{
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public bool Ativo { get; set; } = true;
}
