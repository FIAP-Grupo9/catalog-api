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
