using FCG.BuildingBlocks.Common;
using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Domain.Repositories;

namespace FCG.Catalog.Api.Application.Services;

public class JogoService : IJogoService
{
    private readonly IJogoRepository _repo;

    public JogoService(IJogoRepository repo) => _repo = repo;

    public async Task<Result<IEnumerable<JogoDTO>>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _repo.GetAllAsync(ct);
        return Result<IEnumerable<JogoDTO>>.Success(list.Select(Map));
    }

    public async Task<Result<JogoDTO>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var j = await _repo.GetByIdAsync(id, ct);
        return j is null
            ? Result<JogoDTO>.Failure(Messages.JogoNaoEncontrado, 404)
            : Result<JogoDTO>.Success(Map(j));
    }

    public async Task<Result<JogoDTO>> CreateAsync(CreateJogoDTO dto, CancellationToken ct = default)
    {
        var j = new Jogo
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Preco = dto.Preco,
            Ativo = true,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _repo.AddAsync(j, ct);
        return Result<JogoDTO>.Success(Map(j), 201);
    }

    public async Task<Result<bool>> UpdateAsync(Guid id, UpdateJogoDTO dto, CancellationToken ct = default)
    {
        var j = await _repo.GetByIdAsync(id, ct);
        if (j is null) return Result<bool>.Failure(Messages.JogoNaoEncontrado, 404);

        j.Nome = dto.Nome;
        j.Descricao = dto.Descricao;
        j.Preco = dto.Preco;
        j.Ativo = dto.Ativo;
        j.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(j, ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
        => await _repo.DeleteAsync(id, ct)
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(Messages.JogoNaoEncontrado, 404);

    private static JogoDTO Map(Jogo j) => new()
    {
        Id = j.Id, Nome = j.Nome, Descricao = j.Descricao, Preco = j.Preco, Ativo = j.Ativo
    };
}
