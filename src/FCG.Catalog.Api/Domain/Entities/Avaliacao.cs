using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FCG.Catalog.Api.Domain.Entities;

// Documento MongoDB — não participa do EF Core. Comentário é texto livre de
// tamanho variável e a volumetria cresce sem limite com o uso, então este
// agregado vive no NoSQL enquanto o transacional (jogos, biblioteca,
// promoções) permanece no PostgreSQL.
public class Avaliacao
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid JogoId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UsuarioId { get; set; }

    public string? UsuarioNome { get; set; }

    public int Nota { get; set; }

    public string? Comentario { get; set; }

    public DateTime CreatedAt { get; set; }
}
