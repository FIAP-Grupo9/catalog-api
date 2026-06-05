using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Domain.Enums;

namespace FCG.Catalog.Api.Application.Pricing;

public record PricingResult(decimal OriginalPrice, decimal FinalPrice, Guid? PromocaoId);

public interface IPricingService
{
    PricingResult Calculate(Jogo jogo, Promocao? promocao);
}

public class PricingService : IPricingService
{
    public PricingResult Calculate(Jogo jogo, Promocao? promocao)
    {
        var original = jogo.Preco;

        if (promocao is null || !PromocaoVigente(promocao))
            return new PricingResult(original, original, null);

        var final = promocao.TipoDesconto == TipoDesconto.Percentual
            ? original - (original * promocao.Valor / 100m)
            : original - promocao.Valor;

        if (final < 0) final = 0;
        return new PricingResult(original, final, promocao.Id);
    }

    private static bool PromocaoVigente(Promocao p) =>
        p.IsActive && p.Ativa && DateTime.UtcNow >= p.DataInicio && DateTime.UtcNow <= p.DataFim;
}
