using FCG.Catalog.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Api.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<Promocao> Promocoes => Set<Promocao>();
    public DbSet<BibliotecaJogo> BibliotecaJogos => Set<BibliotecaJogo>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<Jogo>(e =>
        {
            e.ToTable("jogos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nome).HasMaxLength(200);
            e.Property(x => x.Descricao).HasMaxLength(1000);
            e.Property(x => x.Preco).HasColumnType("decimal(18,2)");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
        });

        mb.Entity<Promocao>(e =>
        {
            e.ToTable("promocoes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nome).HasMaxLength(200);
            e.Property(x => x.Valor).HasColumnType("decimal(18,2)");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(x => x.Jogo).WithMany().HasForeignKey(x => x.IdJogo);
        });

        mb.Entity<BibliotecaJogo>(e =>
        {
            e.ToTable("biblioteca_jogos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.PrecoPago).HasColumnType("decimal(18,2)");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(x => x.Jogo).WithMany().HasForeignKey(x => x.JogoId);
            e.HasOne(x => x.Promocao).WithMany().HasForeignKey(x => x.PromocaoId);
            // Idempotência camada 2: garante que um par (UsuarioId, JogoId) ativo seja único.
            e.HasIndex(x => new { x.UsuarioId, x.JogoId })
                .HasFilter("\"IsActive\" = TRUE")
                .IsUnique();
            e.HasIndex(x => x.OrderId).IsUnique();
        });

        mb.Entity<ProcessedMessage>(e =>
        {
            e.ToTable("processed_messages");
            e.HasKey(x => x.MessageId);
            e.Property(x => x.MessageType).HasMaxLength(200);
        });
    }
}
