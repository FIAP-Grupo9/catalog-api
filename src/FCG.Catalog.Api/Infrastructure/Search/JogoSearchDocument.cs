namespace FCG.Catalog.Api.Infrastructure.Search;

public class JogoSearchDocument
{
    public Guid Id { get; set; }
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public bool Ativo { get; set; }
}
