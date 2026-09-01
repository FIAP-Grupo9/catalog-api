using System.Security.Claims;
using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/jogos/{jogoId:guid}/avaliacoes")]
public class AvaliacoesController : ControllerBase
{
    private readonly IAvaliacaoService _avaliacoes;
    public AvaliacoesController(IAvaliacaoService avaliacoes) => _avaliacoes = avaliacoes;

    /// <summary>
    /// Avalia um jogo. Só é permitido para quem possui o jogo na biblioteca, uma vez por jogo.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AvaliacaoDTO>> Criar(Guid jogoId, CreateAvaliacaoDTO dto, CancellationToken ct)
    {
        if (!TryGetCurrent(out var uid, out var nome)) return Unauthorized();
        var r = await _avaliacoes.CriarAsync(jogoId, uid, nome, dto, ct);
        return r.IsSuccess
            ? StatusCode(201, r.Data)
            : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    /// <summary>
    /// Avaliações de um jogo, das mais recentes para as mais antigas.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AvaliacaoDTO>>> Listar(
        Guid jogoId, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var r = await _avaliacoes.ListarPorJogoAsync(jogoId, page, pageSize, ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    /// <summary>
    /// Nota média e total de avaliações do jogo.
    /// </summary>
    [HttpGet("resumo"), AllowAnonymous]
    public async Task<ActionResult<ResumoAvaliacoesDTO>> Resumo(Guid jogoId, CancellationToken ct)
    {
        var r = await _avaliacoes.ResumoPorJogoAsync(jogoId, ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    private bool TryGetCurrent(out Guid id, out string nome)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        nome = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        return Guid.TryParse(idClaim, out id);
    }
}
