using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Domain.Repositories;
using MongoDB.Driver;

namespace FCG.Catalog.Api.Infrastructure.Repositories;

public class AvaliacaoMongoRepository : IAvaliacaoRepository
{
    private readonly IMongoCollection<Avaliacao> _col;

    public AvaliacaoMongoRepository(IMongoClient client, IConfiguration configuration)
    {
        var dbName = configuration["MongoDb:Database"] ?? "catalog";
        _col = client.GetDatabase(dbName).GetCollection<Avaliacao>("avaliacoes");

        // Índice em JogoId: toda consulta filtra por jogo. CreateOne é idempotente.
        _col.Indexes.CreateOne(new CreateIndexModel<Avaliacao>(
            Builders<Avaliacao>.IndexKeys.Ascending(a => a.JogoId).Descending(a => a.CreatedAt)));
    }

    public Task AddAsync(Avaliacao avaliacao, CancellationToken ct = default)
        => _col.InsertOneAsync(avaliacao, cancellationToken: ct);

    public async Task<IReadOnlyList<Avaliacao>> GetByJogoAsync(Guid jogoId, int page, int pageSize, CancellationToken ct = default)
        => await _col.Find(a => a.JogoId == jogoId)
                     .SortByDescending(a => a.CreatedAt)
                     .Skip((page - 1) * pageSize)
                     .Limit(pageSize)
                     .ToListAsync(ct);

    public async Task<(double Media, long Total)> GetResumoAsync(Guid jogoId, CancellationToken ct = default)
    {
        var resumo = await _col.Aggregate()
            .Match(a => a.JogoId == jogoId)
            .Group(a => a.JogoId, g => new { Media = g.Average(x => x.Nota), Total = g.LongCount() })
            .FirstOrDefaultAsync(ct);

        return resumo is null ? (0d, 0L) : (Math.Round(resumo.Media, 2), resumo.Total);
    }

    public Task<bool> UsuarioJaAvaliouAsync(Guid jogoId, Guid usuarioId, CancellationToken ct = default)
        => _col.Find(a => a.JogoId == jogoId && a.UsuarioId == usuarioId).AnyAsync(ct);
}
