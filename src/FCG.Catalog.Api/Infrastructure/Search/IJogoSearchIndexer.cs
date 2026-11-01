using FCG.Catalog.Api.Domain.Entities;

namespace FCG.Catalog.Api.Infrastructure.Search;

// Mantém o índice do Elasticsearch como uma cópia pesquisável do catálogo,
// independente do Postgres. O JogoService chama IndexAsync/DeleteAsync a cada
// escrita para manter os dois sincronizados.
public interface IJogoSearchIndexer
{
    Task EnsureIndexAsync(CancellationToken ct = default);
    Task ReindexAllAsync(IEnumerable<Jogo> jogos, CancellationToken ct = default);
    Task IndexAsync(Jogo jogo, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<JogoSearchHit>> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default);
}

public record JogoSearchHit(JogoSearchDocument Document, double Score);
