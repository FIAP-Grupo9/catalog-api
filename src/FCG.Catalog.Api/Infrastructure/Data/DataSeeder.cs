using FCG.Catalog.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Api.Infrastructure.Data;

/// <summary>
/// Popula o catálogo com alguns jogos. Sem isso, um ambiente recém-criado sobe
/// funcional mas vazio, e o fluxo de compra não pode ser demonstrado sem
/// cadastrar jogos manualmente.
///
/// Desligado por padrão: só age quando SeedData:Enabled está habilitado, e
/// apenas se o catálogo estiver vazio — nunca sobrescreve dados existentes.
/// </summary>
public static class DataSeeder
{
    private static readonly (string Nome, string Descricao, decimal Preco)[] Catalogo =
    [
        ("Ecos do Infinito",  "RPG de ação com mundo aberto e narrativa ramificada.",      79.90m),
        ("Corrida Extrema X", "Corrida com veículos personalizáveis e modo online.",      129.90m),
        ("Reino das Sombras", "Aventura sombria com combate tático por turnos.",           99.50m),
        ("Cidade do Caos",    "Simulador urbano com economia dinâmica e eventos.",         69.99m)
    ];

    public static async Task SeedAsync(
        CatalogDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("SeedData:Enabled"))
            return;

        // O serviço roda com mais de uma réplica, e todas executam este código na
        // subida. Sem a trava, todas encontram a tabela vazia ao mesmo tempo e o
        // catálogo acaba duplicado. A trava é liberada no fim da transação, então
        // as demais réplicas só avaliam a condição depois que a primeira terminou.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(72301)", ct);

        if (await db.Jogos.AnyAsync(ct))
        {
            logger.LogInformation("Carga inicial: catálogo já possui jogos.");
            await tx.CommitAsync(ct);
            return;
        }

        foreach (var (nome, descricao, preco) in Catalogo)
        {
            db.Jogos.Add(new Jogo
            {
                Id = Guid.NewGuid(),
                Nome = nome,
                Descricao = descricao,
                Preco = preco,
                Ativo = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Carga inicial: {Total} jogos criados.", Catalogo.Length);
    }
}
