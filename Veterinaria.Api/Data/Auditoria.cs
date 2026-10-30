using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Data;

public static class AuditoriaExtensions
{
    public static void RegistrarPedido(this VeterinariaDbContext db, long usuarioId, long pedidoId, string? anterior, string nuevo, string? obs = null)
    {
        db.Historiales.Add(new Historial
        {
            PedidoId = pedidoId,
            UsuarioId = usuarioId,
            EstadoAnterior = anterior,
            EstadoNuevo = nuevo,
            Observacion = obs,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        });
    }

    public static void RegistrarEntrega(this VeterinariaDbContext db, long usuarioId, long entregaId, string? anterior, string nuevo, string? obs = null)
    {
        db.Historiales.Add(new Historial
        {
            EntregaId = entregaId,
            UsuarioId = usuarioId,
            EstadoAnterior = anterior,
            EstadoNuevo = nuevo,
            Observacion = obs,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        });
    }
}
