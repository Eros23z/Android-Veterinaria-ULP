using Veterinaria.Api.Domain.Enums;

namespace Veterinaria.Api.Domain.Entities;

public class Notificacion : EntityBase
{
    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public TipoNotificacion Tipo { get; set; } = TipoNotificacion.Informacion;

    public DateTimeOffset? LeidaEn { get; set; }
}
