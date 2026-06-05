using System.ComponentModel.DataAnnotations;

namespace FCG.Catalog.Api.Application.DTOs;

public class BibliotecaJogoDTO
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid JogoId { get; set; }
    public string? JogoNome { get; set; }
    public DateTime DataCompra { get; set; }
    public decimal PrecoPago { get; set; }
    public Guid? PromocaoId { get; set; }
    public Guid OrderId { get; set; }
}

public class CreateBibliotecaJogoDTO
{
    [Required]
    public Guid JogoId { get; set; }

    public Guid? PromocaoId { get; set; }
}

public class OrderPendingDTO
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = "pending";
    public string Message { get; set; } = string.Empty;
}
