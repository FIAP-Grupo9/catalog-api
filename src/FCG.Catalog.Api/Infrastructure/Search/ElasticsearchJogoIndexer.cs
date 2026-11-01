using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using FCG.Catalog.Api.Domain.Entities;

namespace FCG.Catalog.Api.Infrastructure.Search;

public class ElasticsearchJogoIndexer : IJogoSearchIndexer
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;

    public ElasticsearchJogoIndexer(ElasticsearchClient client, IConfiguration configuration)
    {
        _client = client;
        _indexName = configuration["Elasticsearch:IndexName"] ?? "catalog-jogos";
    }

    public async Task EnsureIndexAsync(CancellationToken ct = default)
    {
        var exists = await _client.Indices.ExistsAsync(_indexName, ct);
        if (exists.Exists) return;

        await _client.Indices.CreateAsync(_indexName, c => c
            .Mappings(m => m
                .Properties<JogoSearchDocument>(p => p
                    .Keyword(k => k.Id)
                    .Text(t => t.Nome)
                    .Text(t => t.Descricao)
                    .DoubleNumber(d => d.Preco)
                    .Boolean(b => b.Ativo))),
            ct);
    }

    public async Task ReindexAllAsync(IEnumerable<Jogo> jogos, CancellationToken ct = default)
    {
        var docs = jogos.Select(ToDocument).ToList();
        if (docs.Count == 0) return;

        await _client.BulkAsync(b => b
            .Index(_indexName)
            .IndexMany(docs, (op, doc) => op.Id(doc.Id)),
            ct);
    }

    public async Task IndexAsync(Jogo jogo, CancellationToken ct = default)
    {
        var response = await _client.IndexAsync(ToDocument(jogo), i => i
            .Index(_indexName)
            .Id(jogo.Id),
            ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException($"Falha ao indexar jogo {jogo.Id} no Elasticsearch: {response.DebugInformation}");
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _client.DeleteAsync(_indexName, id.ToString(), ct);
        if (!response.IsValidResponse && response.Result != Elastic.Clients.Elasticsearch.Result.NotFound)
            throw new InvalidOperationException($"Falha ao remover jogo {id} do Elasticsearch: {response.DebugInformation}");
    }

    public async Task<IReadOnlyList<JogoSearchHit>> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default)
    {
        var response = await _client.SearchAsync<JogoSearchDocument>(s => s
            .Indices(_indexName)
            .From((Math.Max(page, 1) - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q
                .Bool(bq => bq
                    .Must(m => m
                        .MultiMatch(mm => mm
                            .Query(query)
                            .Fields(new[] { "nome^2", "descricao" })
                            .Fuzziness(new Fuzziness("AUTO"))))
                    .Filter(f => f
                        .Term(t => t.Field("ativo").Value(true)))))
            .Sort(sort => sort.Score(sc => sc.Order(SortOrder.Desc))),
            ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException($"Falha ao buscar no Elasticsearch: {response.DebugInformation}");

        return response.Hits
            .Select(h => new JogoSearchHit(h.Source!, h.Score ?? 0))
            .ToList();
    }

    private static JogoSearchDocument ToDocument(Jogo jogo) => new()
    {
        Id = jogo.Id,
        Nome = jogo.Nome,
        Descricao = jogo.Descricao,
        Preco = jogo.Preco,
        Ativo = jogo.Ativo
    };
}
