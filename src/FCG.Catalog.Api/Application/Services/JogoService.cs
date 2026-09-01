using System.Text.Json;
using FCG.BuildingBlocks.Common;
using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Domain.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace FCG.Catalog.Api.Application.Services;

public class JogoService : IJogoService
{
    // A listagem completa do catálogo é a consulta mais repetida da API e só muda
    // quando um jogo é criado, alterado ou removido — daí o cache-aside com
    // invalidação explícita na escrita. O TTL é só a rede de segurança.
    private const string CacheKeyAll = "jogos:all";
    private static readonly DistributedCacheEntryOptions CacheOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    private readonly IJogoRepository _repo;
    private readonly IDistributedCache _cache;
    private readonly ILogger<JogoService> _logger;

    public JogoService(IJogoRepository repo, IDistributedCache cache, ILogger<JogoService> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<JogoDTO>>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await TryGetCachedAsync(ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT: {Key}", CacheKeyAll);
            return Result<IEnumerable<JogoDTO>>.Success(cached);
        }

        _logger.LogInformation("Cache MISS: {Key}", CacheKeyAll);
        var list = (await _repo.GetAllAsync(ct)).Select(Map).ToList();
        await TrySetCachedAsync(list, ct);
        return Result<IEnumerable<JogoDTO>>.Success(list);
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
        await InvalidarCacheAsync(ct);
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
        await InvalidarCacheAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _repo.DeleteAsync(id, ct))
            return Result<bool>.Failure(Messages.JogoNaoEncontrado, 404);

        await InvalidarCacheAsync(ct);
        return Result<bool>.Success(true);
    }

    // O cache é acelerador, não fonte de verdade: uma falha do Redis degrada a
    // latência mas não pode derrubar a API, por isso o acesso é tolerante a erro.
    private async Task<List<JogoDTO>?> TryGetCachedAsync(CancellationToken ct)
    {
        try
        {
            var json = await _cache.GetStringAsync(CacheKeyAll, ct);
            return json is null ? null : JsonSerializer.Deserialize<List<JogoDTO>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao ler o cache {Key}; seguindo para o banco.", CacheKeyAll);
            return null;
        }
    }

    private async Task TrySetCachedAsync(List<JogoDTO> list, CancellationToken ct)
    {
        try
        {
            await _cache.SetStringAsync(CacheKeyAll, JsonSerializer.Serialize(list), CacheOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar o cache {Key}.", CacheKeyAll);
        }
    }

    private async Task InvalidarCacheAsync(CancellationToken ct)
    {
        try
        {
            await _cache.RemoveAsync(CacheKeyAll, ct);
            _logger.LogInformation("Cache invalidado: {Key}", CacheKeyAll);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao invalidar o cache {Key}.", CacheKeyAll);
        }
    }

    private static JogoDTO Map(Jogo j) => new()
    {
        Id = j.Id, Nome = j.Nome, Descricao = j.Descricao, Preco = j.Preco, Ativo = j.Ativo
    };
}
