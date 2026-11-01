using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class JogosController : ControllerBase
{
    private readonly IJogoService _jogos;
    public JogosController(IJogoService jogos) => _jogos = jogos;

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<IEnumerable<JogoDTO>>> Todos(CancellationToken ct)
    {
        var r = await _jogos.GetAllAsync(ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    // Busca avançada via Elasticsearch: fuzzy search (tolerante a erros de
    // digitação) nos campos nome/descricao, ordenada por relevância (_score).
    // Independente da consulta tradicional ao Postgres usada pelos demais endpoints.
    [HttpGet("search"), AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SearchJogoDTO>>> Buscar(
        [FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var r = await _jogos.SearchAsync(q, page, pageSize, ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpGet("{id:guid}"), AllowAnonymous]
    public async Task<ActionResult<JogoDTO>> PorId(Guid id, CancellationToken ct)
    {
        var r = await _jogos.GetByIdAsync(id, ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<JogoDTO>> Criar(CreateJogoDTO dto, CancellationToken ct)
    {
        var r = await _jogos.CreateAsync(dto, ct);
        return r.IsSuccess
            ? CreatedAtAction(nameof(PorId), new { id = r.Data!.Id }, r.Data)
            : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Atualizar(Guid id, UpdateJogoDTO dto, CancellationToken ct)
    {
        var r = await _jogos.UpdateAsync(id, dto, ct);
        return r.IsSuccess ? NoContent() : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deletar(Guid id, CancellationToken ct)
    {
        var r = await _jogos.DeleteAsync(id, ct);
        return r.IsSuccess ? NoContent() : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }
}
