using System.ComponentModel.DataAnnotations;

namespace FCG.Catalog.Api.Application.DTOs;

public class AvaliacaoDTO
{
    public string? Id { get; set; }
    public Guid JogoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    public int Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAvaliacaoDTO
{
    [Required, Range(1, 5, ErrorMessage = "Nota precisa estar entre 1 e 5")]
    public int Nota { get; set; }

    [StringLength(2000)]
    public string? Comentario { get; set; }
}

public class ResumoAvaliacoesDTO
{
    public Guid JogoId { get; set; }
    public double Media { get; set; }
    public long Total { get; set; }
}
