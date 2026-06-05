using FCG.BuildingBlocks.Common;
using FCG.Catalog.Api.Application.DTOs;
using FCG.Catalog.Api.Application.Pricing;
using FCG.Catalog.Api.Domain.Repositories;
using FCG.Contracts.Events;
using MassTransit;

namespace FCG.Catalog.Api.Application.Services;

public class BibliotecaJogoService : IBibliotecaJogoService
{
    private readonly IBibliotecaJogoRepository _biblioteca;
    private readonly IJogoRepository _jogos;
    private readonly IPromocaoRepository _promocoes;
    private readonly IPricingService _pricing;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<BibliotecaJogoService> _logger;

    public BibliotecaJogoService(
        IBibliotecaJogoRepository biblioteca,
        IJogoRepository jogos,
        IPromocaoRepository promocoes,
        IPricingService pricing,
        IPublishEndpoint publisher,
        ILogger<BibliotecaJogoService> logger)
    {
        _biblioteca = biblioteca;
        _jogos = jogos;
        _promocoes = promocoes;
        _pricing = pricing;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<BibliotecaJogoDTO>>> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var items = await _biblioteca.GetByUsuarioIdAsync(usuarioId, ct);
        return Result<IEnumerable<BibliotecaJogoDTO>>.Success(items.Select(Map));
    }

    public async Task<Result<BibliotecaJogoDTO>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _biblioteca.GetByIdAsync(id, ct);
        return item is null
            ? Result<BibliotecaJogoDTO>.Failure(Messages.ItemNaoEncontrado, 404)
            : Result<BibliotecaJogoDTO>.Success(Map(item));
    }

    // Fluxo invertido (vs. monolito): NÃO grava BibliotecaJogo aqui.
    // Apenas valida, calcula preço final e publica OrderPlacedEvent.
    // A escrita acontece no consumer de PaymentProcessedEvent.
    public async Task<Result<OrderPendingDTO>> IniciarCompraAsync(
        Guid usuarioId,
        string userEmail,
        string userName,
        CreateBibliotecaJogoDTO dto,
        CancellationToken ct = default)
    {
        var jaTem = await _biblioteca.FindActiveAsync(usuarioId, dto.JogoId, ct);
        if (jaTem is not null)
            return Result<OrderPendingDTO>.Failure(Messages.JogoJaCadastradoNaBiblioteca, 409);

        var jogo = await _jogos.GetByIdAsync(dto.JogoId, ct);
        if (jogo is null) return Result<OrderPendingDTO>.Failure(Messages.JogoNaoEncontrado, 404);
        if (!jogo.Ativo) return Result<OrderPendingDTO>.Failure(Messages.JogoInativo, 400);

        var promocao = dto.PromocaoId.HasValue
            ? await _promocoes.GetByIdAsync(dto.PromocaoId.Value, ct)
            : null;

        if (dto.PromocaoId.HasValue && promocao is null)
            return Result<OrderPendingDTO>.Failure(Messages.PromocaoInvalidaOuExpirada, 400);

        var pricing = _pricing.Calculate(jogo, promocao);
        if (dto.PromocaoId.HasValue && pricing.PromocaoId is null)
            return Result<OrderPendingDTO>.Failure(Messages.PromocaoInvalidaOuExpirada, 400);

        var orderId = Guid.NewGuid();
        var evt = new OrderPlacedEvent(
            MessageId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            OrderId: orderId,
            UserId: usuarioId,
            UserEmail: userEmail,
            UserName: userName,
            GameId: jogo.Id,
            GameName: jogo.Nome ?? string.Empty,
            OriginalPrice: pricing.OriginalPrice,
            FinalPrice: pricing.FinalPrice,
            PromocaoId: pricing.PromocaoId);

        await _publisher.Publish(evt, ct);

        _logger.LogInformation(
            "OrderPlacedEvent publicado. OrderId={OrderId}, UserId={UserId}, GameId={GameId}, FinalPrice={FinalPrice}",
            orderId, usuarioId, jogo.Id, pricing.FinalPrice);

        return Result<OrderPendingDTO>.Success(new OrderPendingDTO
        {
            OrderId = orderId,
            Status = "pending",
            Message = "Compra iniciada. Acompanhe o status; em caso de aprovação, o jogo será adicionado à sua biblioteca."
        }, 202);
    }

    private static BibliotecaJogoDTO Map(Domain.Entities.BibliotecaJogo b) => new()
    {
        Id = b.Id,
        UsuarioId = b.UsuarioId,
        JogoId = b.JogoId,
        JogoNome = b.Jogo?.Nome,
        DataCompra = b.DataCompra,
        PrecoPago = b.PrecoPago,
        PromocaoId = b.PromocaoId,
        OrderId = b.OrderId
    };
}
