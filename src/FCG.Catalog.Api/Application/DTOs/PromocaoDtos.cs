using System.ComponentModel.DataAnnotations;
using FCG.Catalog.Api.Domain.Enums;

namespace FCG.Catalog.Api.Application.DTOs;

public class PromocaoDTO
{
    public Guid Id { get; set; }
    public string? Nome { get; set; }
    public Guid IdJogo { get; set; }
    public string? JogoNome { get; set; }
    public TipoDesconto TipoDesconto { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public bool Ativa { get; set; }
}

public class CreatePromocaoDTO
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string? Nome { get; set; }

    [Required]
    public Guid IdJogo { get; set; }

    [Required]
    public TipoDesconto TipoDesconto { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Valor { get; set; }

    [Required]
    public DateTime DataInicio { get; set; }

    [Required]
    public DateTime DataFim { get; set; }
}

public class UpdatePromocaoDTO
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string? Nome { get; set; }

    [Required]
    public Guid IdJogo { get; set; }

    [Required]
    public TipoDesconto TipoDesconto { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Valor { get; set; }

    [Required]
    public DateTime DataInicio { get; set; }

    [Required]
    public DateTime DataFim { get; set; }

    public bool? Ativa { get; set; }
}
