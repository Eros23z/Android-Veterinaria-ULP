using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Auth;
using Veterinaria.Api.Data;

namespace Veterinaria.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificacionesController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public NotificacionesController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotificaciones(
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        var usuarioId = User.UsuarioId();
        if (!usuarioId.HasValue)
        {
            return Unauthorized();
        }

        var paginaActual = pagina.GetValueOrDefault(1);
        var tamanoActual = tamano.GetValueOrDefault(10);
        if (paginaActual < 1) paginaActual = 1;
        if (tamanoActual < 1) tamanoActual = 10;

        var queryBase = _context.Notificaciones
            .Where(x => x.Activo && x.UsuarioId == usuarioId.Value);

        var total = await queryBase.CountAsync();
        var noLeidas = await queryBase.CountAsync(x => x.LeidaEn == null);

        var items = await queryBase
            .OrderByDescending(x => x.CreadoEn)
            .Skip((paginaActual - 1) * tamanoActual)
            .Take(tamanoActual)
            .Select(x => new
            {
                id = x.Id,
                usuario_id = x.UsuarioId,
                titulo = x.Titulo,
                mensaje = x.Mensaje,
                tipo = x.Tipo,
                leida_en = x.LeidaEn,
                es_leida = x.LeidaEn != null,
                creado_en = x.CreadoEn
            })
            .ToListAsync();

        var hayMas = ((paginaActual - 1) * tamanoActual) + items.Count < total;

        return Ok(new
        {
            notificaciones = items,
            resumen = new
            {
                total,
                no_leidas = noLeidas
            },
            pagina = new
            {
                pagina = paginaActual,
                tamano = tamanoActual,
                total,
                hay_mas = hayMas
            }
        });
    }

    [HttpPut("{id}/leer")]
    public async Task<IActionResult> MarcarLeida(long id)
    {
        var usuarioId = User.UsuarioId();
        if (!usuarioId.HasValue)
        {
            return Unauthorized();
        }

        var notificacion = await _context.Notificaciones
            .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId.Value && x.Activo);

        if (notificacion == null)
        {
            return NotFound(new { codigo = "notificacion_no_encontrada", mensaje = "La notificación no existe." });
        }

        if (notificacion.LeidaEn == null)
        {
            notificacion.LeidaEn = DateTimeOffset.UtcNow;
            notificacion.ActualizadoEn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            id = notificacion.Id,
            leida_en = notificacion.LeidaEn,
            mensaje = "Notificación marcada como leída."
        });
    }

    [HttpPut("marcar-todas")]
    public async Task<IActionResult> MarcarTodasLeidas()
    {
        var usuarioId = User.UsuarioId();
        if (!usuarioId.HasValue)
        {
            return Unauthorized();
        }

        var pendientes = await _context.Notificaciones
            .Where(x => x.Activo && x.UsuarioId == usuarioId.Value && x.LeidaEn == null)
            .ToListAsync();

        var ahora = DateTimeOffset.UtcNow;
        var ahoraUtc = DateTime.UtcNow;

        foreach (var notif in pendientes)
        {
            notif.LeidaEn = ahora;
            notif.ActualizadoEn = ahoraUtc;
        }

        if (pendientes.Any())
        {
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            marcadas = pendientes.Count,
            mensaje = "Todas las notificaciones han sido marcadas como leídas."
        });
    }
}
