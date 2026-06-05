using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PromocoesController : ControllerBase
{
    private readonly IPromocaoService _promocoes;
    public PromocoesController(IPromocaoService promocoes) => _promocoes = promocoes;

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PromocaoDTO>>> Todas(CancellationToken ct)
    {
        var r = await _promocoes.GetAllAsync(ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpGet("{id:guid}"), AllowAnonymous]
    public async Task<ActionResult<PromocaoDTO>> PorId(Guid id, CancellationToken ct)
    {
        var r = await _promocoes.GetByIdAsync(id, ct);
        return r.IsSuccess ? Ok(r.Data) : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<PromocaoDTO>> Criar(CreatePromocaoDTO dto, CancellationToken ct)
    {
        var r = await _promocoes.CreateAsync(dto, ct);
        return r.IsSuccess
            ? CreatedAtAction(nameof(PorId), new { id = r.Data!.Id }, r.Data)
            : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Atualizar(Guid id, UpdatePromocaoDTO dto, CancellationToken ct)
    {
        var r = await _promocoes.UpdateAsync(id, dto, ct);
        return r.IsSuccess ? NoContent() : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deletar(Guid id, CancellationToken ct)
    {
        var r = await _promocoes.DeleteAsync(id, ct);
        return r.IsSuccess ? NoContent() : StatusCode(r.StatusCode, new { message = r.ErrorMessage });
    }
}
