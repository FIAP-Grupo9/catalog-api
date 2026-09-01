using FCG.BuildingBlocks.Common;
using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Domain.Repositories;

namespace FCG.Catalog.Api.Application.Services;

public class AvaliacaoService : IAvaliacaoService
{
    private readonly IAvaliacaoRepository _repo;
    private readonly IJogoRepository _jogos;
    private readonly IBibliotecaJogoRepository _biblioteca;

    public AvaliacaoService(
        IAvaliacaoRepository repo,
        IJogoRepository jogos,
        IBibliotecaJogoRepository biblioteca)
    {
        _repo = repo;
        _jogos = jogos;
        _biblioteca = biblioteca;
    }

    public async Task<Result<AvaliacaoDTO>> CriarAsync(
        Guid jogoId, Guid usuarioId, string usuarioNome, CreateAvaliacaoDTO dto, CancellationToken ct = default)
    {
        var jogo = await _jogos.GetByIdAsync(jogoId, ct);
        if (jogo is null) return Result<AvaliacaoDTO>.Failure(Messages.JogoNaoEncontrado, 404);

        var possui = await _biblioteca.FindActiveAsync(usuarioId, jogoId, ct);
        if (possui is null) return Result<AvaliacaoDTO>.Failure(Messages.JogoNaoEstaNaBiblioteca, 403);

        if (await _repo.UsuarioJaAvaliouAsync(jogoId, usuarioId, ct))
            return Result<AvaliacaoDTO>.Failure(Messages.JogoJaAvaliado, 409);

        var avaliacao = new Avaliacao
        {
            JogoId = jogoId,
            UsuarioId = usuarioId,
            UsuarioNome = usuarioNome,
            Nota = dto.Nota,
            Comentario = dto.Comentario,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(avaliacao, ct);
        return Result<AvaliacaoDTO>.Success(Map(avaliacao), 201);
    }

    public async Task<Result<IEnumerable<AvaliacaoDTO>>> ListarPorJogoAsync(
        Guid jogoId, int page, int pageSize, CancellationToken ct = default)
    {
        var jogo = await _jogos.GetByIdAsync(jogoId, ct);
        if (jogo is null) return Result<IEnumerable<AvaliacaoDTO>>.Failure(Messages.JogoNaoEncontrado, 404);

        var list = await _repo.GetByJogoAsync(jogoId, page, pageSize, ct);
        return Result<IEnumerable<AvaliacaoDTO>>.Success(list.Select(Map));
    }

    public async Task<Result<ResumoAvaliacoesDTO>> ResumoPorJogoAsync(Guid jogoId, CancellationToken ct = default)
    {
        var jogo = await _jogos.GetByIdAsync(jogoId, ct);
        if (jogo is null) return Result<ResumoAvaliacoesDTO>.Failure(Messages.JogoNaoEncontrado, 404);

        var (media, total) = await _repo.GetResumoAsync(jogoId, ct);
        return Result<ResumoAvaliacoesDTO>.Success(new ResumoAvaliacoesDTO
        {
            JogoId = jogoId,
            Media = media,
            Total = total
        });
    }

    private static AvaliacaoDTO Map(Avaliacao a) => new()
    {
        Id = a.Id, JogoId = a.JogoId, UsuarioId = a.UsuarioId, UsuarioNome = a.UsuarioNome,
        Nota = a.Nota, Comentario = a.Comentario, CreatedAt = a.CreatedAt
    };
}
