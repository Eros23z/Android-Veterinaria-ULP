using System.Text.Json.Serialization;

namespace Veterinaria.Api.Domain.Entities;

public class RefreshToken : EntityBase
{
    public long UsuarioId { get; set; }

    [JsonIgnore]
    public Usuario Usuario { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset? RevocadoEn { get; set; }

    public bool EsActivo => RevocadoEn == null && DateTimeOffset.UtcNow < ExpiraEn;
}
