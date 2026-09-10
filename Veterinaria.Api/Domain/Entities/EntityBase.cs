namespace Veterinaria.Api.Domain.Entities;

public abstract class EntityBase
{
    public long Id { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
}
