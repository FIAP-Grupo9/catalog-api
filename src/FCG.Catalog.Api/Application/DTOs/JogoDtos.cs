using System.ComponentModel.DataAnnotations;

namespace FCG.Catalog.Api.Application.DTOs;

public class JogoDTO
{
    public Guid Id { get; set; }
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public bool Ativo { get; set; }
}

public class CreateJogoDTO
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string? Nome { get; set; }

    [StringLength(1000)]
    public string? Descricao { get; set; }

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Preço precisa ser maior que zero")]
    public decimal Preco { get; set; }
}

public class UpdateJogoDTO
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string? Nome { get; set; }

    [StringLength(1000)]
    public string? Descricao { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Preco { get; set; }

    public bool Ativo { get; set; }
}
