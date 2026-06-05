using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Infrastructure.Data;
using FCG.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Api.Infrastructure.Consumers;

// Consome PaymentProcessedEvent e, se Approved, materializa BibliotecaJogo
// na MESMA transação em que registra ProcessedMessage — garante idempotência.
public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(CatalogDbContext db, ILogger<PaymentProcessedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var e = context.Message;

        var alreadyProcessed = await _db.ProcessedMessages
            .AnyAsync(p => p.MessageId == e.MessageId, context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "PaymentProcessedEvent ignorado (já processado). MessageId={MessageId}, OrderId={OrderId}",
                e.MessageId, e.OrderId);
            return;
        }

        if (e.Status != PaymentStatus.Approved)
        {
            _db.ProcessedMessages.Add(new ProcessedMessage
            {
                MessageId = e.MessageId,
                MessageType = nameof(PaymentProcessedEvent),
                ProcessedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Pagamento rejeitado para OrderId={OrderId}, motivo='{Reason}'. Biblioteca NÃO atualizada.",
                e.OrderId, e.FailureReason);
            return;
        }

        // Status == Approved: insere BibliotecaJogo + ProcessedMessage atomicamente.
        var jaTem = await _db.BibliotecaJogos
            .AnyAsync(b => b.UsuarioId == e.UserId && b.JogoId == e.GameId && b.IsActive,
                context.CancellationToken);

        if (jaTem)
        {
            _logger.LogWarning(
                "Pagamento aprovado mas usuário já possui o jogo (race ou duplicação). OrderId={OrderId}, UserId={UserId}, GameId={GameId}",
                e.OrderId, e.UserId, e.GameId);
        }
        else
        {
            _db.BibliotecaJogos.Add(new BibliotecaJogo
            {
                Id = Guid.NewGuid(),
                UsuarioId = e.UserId,
                JogoId = e.GameId,
                DataCompra = e.ProcessedAt,
                PrecoPago = e.Amount,
                PromocaoId = e.PromocaoId,
                OrderId = e.OrderId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        _db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = e.MessageId,
            MessageType = nameof(PaymentProcessedEvent),
            ProcessedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Biblioteca atualizada para UserId={UserId}, GameId={GameId}, OrderId={OrderId}",
            e.UserId, e.GameId, e.OrderId);
    }
}
