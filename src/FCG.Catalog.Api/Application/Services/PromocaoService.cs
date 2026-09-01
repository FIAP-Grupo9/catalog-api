using System.Text.Json;
using FCG.BuildingBlocks.Common;
using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Domain.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace FCG.Catalog.Api.Application.Services;

public class PromocaoService : IPromocaoService
{
    private const string CacheKeyAll = "promocoes:all";
    private static readonly DistributedCacheEntryOptions CacheOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    private readonly IPromocaoRepository _repo;
    private readonly IJogoRepository _jogos;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PromocaoService> _logger;

    public PromocaoService(
        IPromocaoRepository repo,
        IJogoRepository jogos,
        IDistributedCache cache,
        ILogger<PromocaoService> logger)
    {
        _repo = repo;
        _jogos = jogos;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PromocaoDTO>>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await TryGetCachedAsync(ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT: {Key}", CacheKeyAll);
            return Result<IEnumerable<PromocaoDTO>>.Success(cached);
        }

        _logger.LogInformation("Cache MISS: {Key}", CacheKeyAll);
        var list = (await _repo.GetAllAsync(ct)).Select(Map).ToList();
        await TrySetCachedAsync(list, ct);
        return Result<IEnumerable<PromocaoDTO>>.Success(list);
    }

    public async Task<Result<PromocaoDTO>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        return p is null
            ? Result<PromocaoDTO>.Failure(Messages.PromocaoNaoEncontrada, 404)
            : Result<PromocaoDTO>.Success(Map(p));
    }

    public async Task<Result<PromocaoDTO>> CreateAsync(CreatePromocaoDTO dto, CancellationToken ct = default)
    {
        if (dto.DataFim <= dto.DataInicio)
            return Result<PromocaoDTO>.Failure(Messages.DataFimDeveSerAposDataInicio, 400);

        var jogo = await _jogos.GetByIdAsync(dto.IdJogo, ct);
        if (jogo is null) return Result<PromocaoDTO>.Failure(Messages.JogoNaoEncontrado, 404);

        var p = new Promocao
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            IdJogo = dto.IdJogo,
            TipoDesconto = dto.TipoDesconto,
            Valor = dto.Valor,
            DataInicio = dto.DataInicio,
            DataFim = dto.DataFim,
            Ativa = true,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _repo.AddAsync(p, ct);
        await InvalidarCacheAsync(ct);
        p.Jogo = jogo;
        return Result<PromocaoDTO>.Success(Map(p), 201);
    }

    public async Task<Result<bool>> UpdateAsync(Guid id, UpdatePromocaoDTO dto, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        if (p is null) return Result<bool>.Failure(Messages.PromocaoNaoEncontrada, 404);
        if (dto.DataFim <= dto.DataInicio) return Result<bool>.Failure(Messages.DataFimDeveSerAposDataInicio, 400);

        var jogo = await _jogos.GetByIdAsync(dto.IdJogo, ct);
        if (jogo is null) return Result<bool>.Failure(Messages.JogoNaoEncontrado, 404);

        p.Nome = dto.Nome ?? p.Nome;
        p.IdJogo = dto.IdJogo;
        p.TipoDesconto = dto.TipoDesconto;
        p.Valor = dto.Valor > 0 ? dto.Valor : p.Valor;
        p.DataInicio = dto.DataInicio;
        p.DataFim = dto.DataFim;
        p.Ativa = dto.Ativa ?? p.Ativa;
        p.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(p, ct);
        await InvalidarCacheAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _repo.DeleteAsync(id, ct))
            return Result<bool>.Failure(Messages.PromocaoNaoEncontrada, 404);

        await InvalidarCacheAsync(ct);
        return Result<bool>.Success(true);
    }

    private async Task<List<PromocaoDTO>?> TryGetCachedAsync(CancellationToken ct)
    {
        try
        {
            var json = await _cache.GetStringAsync(CacheKeyAll, ct);
            return json is null ? null : JsonSerializer.Deserialize<List<PromocaoDTO>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao ler o cache {Key}; seguindo para o banco.", CacheKeyAll);
            return null;
        }
    }

    private async Task TrySetCachedAsync(List<PromocaoDTO> list, CancellationToken ct)
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

    private static PromocaoDTO Map(Promocao p) => new()
    {
        Id = p.Id, Nome = p.Nome, IdJogo = p.IdJogo, JogoNome = p.Jogo?.Nome,
        TipoDesconto = p.TipoDesconto, Valor = p.Valor,
        DataInicio = p.DataInicio, DataFim = p.DataFim, Ativa = p.Ativa
    };
}
