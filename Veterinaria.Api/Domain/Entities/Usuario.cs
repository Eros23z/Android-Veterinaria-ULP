using System.Text.Json.Serialization;

namespace Veterinaria.Api.Domain.Entities;

public class Usuario : EntityBase
{
    public long? RolId { get; set; }
    public Rol? Rol { get; set; }

    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
