using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Domain.Repositories;
using FCG.Catalog.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Api.Infrastructure.Repositories;

public class JogoRepository : IJogoRepository
{
    private readonly CatalogDbContext _db;
    public JogoRepository(CatalogDbContext db) => _db = db;

    public Task<Jogo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Jogos.FirstOrDefaultAsync(j => j.Id == id && j.IsActive, ct);

    public async Task<IEnumerable<Jogo>> GetAllAsync(CancellationToken ct = default)
        => await _db.Jogos.Where(j => j.IsActive).ToListAsync(ct);

    public async Task AddAsync(Jogo entity, CancellationToken ct = default)
    {
        await _db.Jogos.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAsync(Jogo entity, CancellationToken ct = default)
    {
        _db.Jogos.Update(entity);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var j = await _db.Jogos.FindAsync(new object[] { id }, ct);
        if (j is null) return false;
        j.IsActive = false;
        j.UpdatedAt = DateTime.UtcNow;
        return await _db.SaveChangesAsync(ct) > 0;
    }
}

public class PromocaoRepository : IPromocaoRepository
{
    private readonly CatalogDbContext _db;
    public PromocaoRepository(CatalogDbContext db) => _db = db;

    public Task<Promocao?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Promocoes.Include(p => p.Jogo).FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);

    public async Task<IEnumerable<Promocao>> GetAllAsync(CancellationToken ct = default)
        => await _db.Promocoes.Include(p => p.Jogo).Where(p => p.IsActive).ToListAsync(ct);

    public async Task AddAsync(Promocao entity, CancellationToken ct = default)
    {
        await _db.Promocoes.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAsync(Promocao entity, CancellationToken ct = default)
    {
        _db.Promocoes.Update(entity);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.Promocoes.FindAsync(new object[] { id }, ct);
        if (p is null) return false;
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;
        return await _db.SaveChangesAsync(ct) > 0;
    }
}

public class BibliotecaJogoRepository : IBibliotecaJogoRepository
{
    private readonly CatalogDbContext _db;
    public BibliotecaJogoRepository(CatalogDbContext db) => _db = db;

    public Task<BibliotecaJogo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.BibliotecaJogos.Include(b => b.Jogo)
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive, ct);

    public async Task<IEnumerable<BibliotecaJogo>> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default)
        => await _db.BibliotecaJogos.Include(b => b.Jogo)
            .Where(b => b.UsuarioId == usuarioId && b.IsActive)
            .ToListAsync(ct);

    public Task<BibliotecaJogo?> FindActiveAsync(Guid usuarioId, Guid jogoId, CancellationToken ct = default)
        => _db.BibliotecaJogos.FirstOrDefaultAsync(
            b => b.UsuarioId == usuarioId && b.JogoId == jogoId && b.IsActive, ct);

    public Task<BibliotecaJogo?> FindByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => _db.BibliotecaJogos.FirstOrDefaultAsync(b => b.OrderId == orderId, ct);
}
