using System.Security.Claims;
using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BibliotecaController : ControllerBase
{
    private readonly IBibliotecaJogoService _biblioteca;
    public BibliotecaController(IBibliotecaJogoService biblioteca) => _biblioteca = biblioteca;

    /// <summary>
    /// Biblioteca do usuário logado.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BibliotecaJogoDTO>>> Minha(CancellationToken ct)
    {
        if (!TryGetCurrent(out var uid, out _, out _)) return Unauthorized();
        var r = await _biblioteca.GetByUsuarioIdAsync(uid, ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BibliotecaJogoDTO>> PorId(Guid id, CancellationToken ct)
    {
        var r = await _biblioteca.GetByIdAsync(id, ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    /// <summary>
    /// Inicia uma compra. Retorna 202 Accepted; o jogo é adicionado à biblioteca após o pagamento ser aprovado de forma assíncrona.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderPendingDTO>> IniciarCompra(CreateBibliotecaJogoDTO dto, CancellationToken ct)
    {
        if (!TryGetCurrent(out var uid, out var email, out var name)) return Unauthorized();
        var r = await _biblioteca.IniciarCompraAsync(uid, email, name, dto, ct);
        return r.IsSuccess
            ? Accepted(r.Data)
            : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    private bool TryGetCurrent(out Guid id, out string email, out string name)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        name = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        return Guid.TryParse(idClaim, out id);
    }
}
