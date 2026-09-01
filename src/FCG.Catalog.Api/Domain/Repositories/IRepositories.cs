using FCG.Catalog.Api.Domain.Entities;

namespace FCG.Catalog.Api.Domain.Repositories;

public interface IJogoRepository
{
    Task<Jogo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Jogo>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Jogo entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(Jogo entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IPromocaoRepository
{
    Task<Promocao?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Promocao>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Promocao entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(Promocao entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IBibliotecaJogoRepository
{
    Task<BibliotecaJogo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<BibliotecaJogo>> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default);
    Task<BibliotecaJogo?> FindActiveAsync(Guid usuarioId, Guid jogoId, CancellationToken ct = default);
    Task<BibliotecaJogo?> FindByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}

// Persistido no MongoDB, e não via EF Core: os métodos recebem/retornam a
// entidade diretamente, sem DbContext nem unidade de trabalho compartilhada.
public interface IAvaliacaoRepository
{
    Task AddAsync(Avaliacao avaliacao, CancellationToken ct = default);
    Task<IReadOnlyList<Avaliacao>> GetByJogoAsync(Guid jogoId, int page, int pageSize, CancellationToken ct = default);
    Task<(double Media, long Total)> GetResumoAsync(Guid jogoId, CancellationToken ct = default);
    Task<bool> UsuarioJaAvaliouAsync(Guid jogoId, Guid usuarioId, CancellationToken ct = default);
}
