using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Auth;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;
using Veterinaria.Api.Domain.Enums;

namespace Veterinaria.Api.Controllers;

[Authorize(Roles = "ADMIN,VETERINARIO")]
[ApiController]
[Route("api/[controller]")]
public class EntregasController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    // Transiciones válidas según máquina de estados de la cátedra
    private static readonly Dictionary<EstadoEntrega, List<EstadoEntrega>> TransicionesValidas = new()
    {
        { EstadoEntrega.Pendiente, new List<EstadoEntrega> { EstadoEntrega.Asignada, EstadoEntrega.Cancelada } },
        { EstadoEntrega.Asignada, new List<EstadoEntrega> { EstadoEntrega.EnCamino, EstadoEntrega.Cancelada } },
        { EstadoEntrega.EnCamino, new List<EstadoEntrega> { EstadoEntrega.Entregada, EstadoEntrega.Cancelada } },
        { EstadoEntrega.Entregada, new List<EstadoEntrega>() },
        { EstadoEntrega.Cancelada, new List<EstadoEntrega>() },
    };

    public EntregasController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEntregas(
        [FromQuery] string? estados,
        [FromQuery] bool? solo_mias,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        var paginaActual = pagina.GetValueOrDefault(1);
        var tamanoActual = tamano.GetValueOrDefault(10);
        if (paginaActual < 1) paginaActual = 1;
        if (tamanoActual < 1) tamanoActual = 10;

        var usuarioActualId = User.UsuarioId();

        var queryBase = _context.Entregas
            .Include(e => e.Pedido)
                .ThenInclude(p => p.Cliente)
            .Include(e => e.Repartidor)
            .Where(e => e.Activo);

        // Contadores de resumen global
        var contadoresQuery = queryBase;
        if (solo_mias == true && usuarioActualId.HasValue)
        {
            contadoresQuery = contadoresQuery.Where(e => e.RepartidorId == usuarioActualId.Value);
        }

        var agrupados = await contadoresQuery
            .GroupBy(e => e.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var resumen = new
        {
            total = agrupados.Sum(g => g.Cantidad),
            pendientes = agrupados.FirstOrDefault(g => g.Estado == EstadoEntrega.Pendiente)?.Cantidad ?? 0,
            asignadas = agrupados.FirstOrDefault(g => g.Estado == EstadoEntrega.Asignada)?.Cantidad ?? 0,
            en_camino = agrupados.FirstOrDefault(g => g.Estado == EstadoEntrega.EnCamino)?.Cantidad ?? 0,
            entregadas = agrupados.FirstOrDefault(g => g.Estado == EstadoEntrega.Entregada)?.Cantidad ?? 0,
            canceladas = agrupados.FirstOrDefault(g => g.Estado == EstadoEntrega.Cancelada)?.Cantidad ?? 0,
        };

        var query = queryBase;

        if (solo_mias == true && usuarioActualId.HasValue)
        {
            query = query.Where(e => e.RepartidorId == usuarioActualId.Value);
        }

        if (!string.IsNullOrWhiteSpace(estados))
        {
            var estadosLista = estados.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<EstadoEntrega>(s, true, out var est) ? (EstadoEntrega?)est : null)
                .Where(e => e.HasValue)
                .Select(e => e!.Value)
                .ToList();

            if (estadosLista.Any())
            {
                query = query.Where(e => estadosLista.Contains(e.Estado));
            }
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.CreadoEn)
            .Skip((paginaActual - 1) * tamanoActual)
            .Take(tamanoActual)
            .Select(e => new
            {
                id = e.Id,
                pedido_id = e.PedidoId,
                codigo_pedido = $"PED-{e.PedidoId:D4}",
                repartidor_id = e.RepartidorId,
                repartidor_email = e.Repartidor != null ? e.Repartidor.Email : null,
                cliente_id = e.Pedido.ClienteId,
                cliente_nombre = e.Pedido.Cliente.Nombre,
                cliente_telefono = e.Pedido.Cliente.Telefono,
                cliente_direccion = e.Pedido.Cliente.Direccion,
                cliente_foto_url = e.Pedido.Cliente.FotoUrl,
                direccion_linea = e.DireccionLinea,
                direccion_referencia = e.DireccionReferencia,
                direccion_latitud = e.DireccionLatitud,
                direccion_longitud = e.DireccionLongitud,
                estado = e.Estado,
                total = e.Pedido.Total,
                fecha_pedido = e.Pedido.FechaPedido,
                asignada_en = e.AsignadaEn,
                entregada_en = e.EntregadaEn,
                observaciones = e.Observaciones,
                creado_en = e.CreadoEn,
                actualizado_en = e.ActualizadoEn
            })
            .ToListAsync();

        var hayMas = ((paginaActual - 1) * tamanoActual) + items.Count < total;

        return Ok(new
        {
            entregas = items,
            resumen,
            pagina = new
            {
                pagina = paginaActual,
                tamano = tamanoActual,
                total,
                hay_mas = hayMas
            }
        });
    }

    [HttpPut("{id}/asignar")]
    public async Task<IActionResult> Asignar(long id, [FromBody] AsignarEntregaDto? dto)
    {
        var entrega = await _context.Entregas
            .Include(e => e.Pedido)
            .FirstOrDefaultAsync(e => e.Id == id && e.Activo);

        if (entrega == null)
        {
            return NotFound(new { codigo = "entrega_no_encontrada", mensaje = "La entrega especificada no existe." });
        }

        var usuarioActualId = User.UsuarioId();
        if (!usuarioActualId.HasValue)
        {
            return Unauthorized();
        }

        long repartidorDestinoId = dto?.RepartidorId ?? usuarioActualId.Value;

        // Si se asigna a otro repartidor distinto al usuario autenticado, debe ser ADMIN
        if (repartidorDestinoId != usuarioActualId.Value && !User.IsInRole("ADMIN"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                codigo = "permisos_insuficientes",
                mensaje = "Solo los administradores pueden asignar entregas a otros usuarios."
            });
        }

        var repartidor = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == repartidorDestinoId && u.Activo);
        if (repartidor == null)
        {
            return BadRequest(new { codigo = "repartidor_no_encontrado", mensaje = "El repartidor especificado no existe o está inactivo." });
        }

        entrega.RepartidorId = repartidorDestinoId;
        entrega.Estado = EstadoEntrega.Asignada;
        entrega.AsignadaEn = DateTimeOffset.UtcNow;
        entrega.ActualizadoEn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = entrega.Id,
            estado = entrega.Estado,
            repartidor_id = entrega.RepartidorId,
            asignada_en = entrega.AsignadaEn,
            mensaje = "Entrega asignada correctamente."
        });
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(long id, [FromBody] CambiarEstadoEntregaDto dto)
    {
        var entrega = await _context.Entregas
            .Include(e => e.Pedido)
            .FirstOrDefaultAsync(e => e.Id == id && e.Activo);

        if (entrega == null)
        {
            return NotFound(new { codigo = "entrega_no_encontrada", mensaje = "La entrega especificada no existe." });
        }

        if (entrega.Estado == dto.Estado)
        {
            return Ok(new { id = entrega.Id, estado = entrega.Estado, mensaje = "La entrega ya se encuentra en dicho estado." });
        }

        if (!TransicionesValidas.TryGetValue(entrega.Estado, out var permitidos) || !permitidos.Contains(dto.Estado))
        {
            return BadRequest(new
            {
                codigo = "transicion_invalida",
                mensaje = $"No está permitida la transición de estado desde '{entrega.Estado}' hacia '{dto.Estado}'."
            });
        }

        entrega.Estado = dto.Estado;
        entrega.ActualizadoEn = DateTime.UtcNow;

        if (dto.Estado == EstadoEntrega.Entregada)
        {
            entrega.EntregadaEn = DateTimeOffset.UtcNow;
            if (entrega.Pedido != null)
            {
                entrega.Pedido.Estado = EstadoPedido.Entregado;
                entrega.Pedido.ActualizadoEn = DateTime.UtcNow;
            }
        }
        else if (dto.Estado == EstadoEntrega.Cancelada)
        {
            if (entrega.Pedido != null)
            {
                entrega.Pedido.Estado = EstadoPedido.Cancelado;
                entrega.Pedido.ActualizadoEn = DateTime.UtcNow;
            }
        }

        // Al pasar a Entregada o Cancelada, emitir Notificacion al creador del pedido
        if ((dto.Estado == EstadoEntrega.Entregada || dto.Estado == EstadoEntrega.Cancelada) &&
            entrega.Pedido?.UsuarioCreadorId.HasValue == true)
        {
            var notificacion = new Notificacion
            {
                UsuarioId = entrega.Pedido.UsuarioCreadorId.Value,
                Titulo = $"Entrega PED-{entrega.PedidoId:D4} {dto.Estado}",
                Mensaje = $"La entrega del pedido PED-{entrega.PedidoId:D4} ha sido marcada como '{dto.Estado}'.",
                Tipo = TipoNotificacion.Entrega,
                Activo = true,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };
            _context.Notificaciones.Add(notificacion);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = entrega.Id,
            estado = entrega.Estado,
            entregada_en = entrega.EntregadaEn,
            mensaje = $"Estado de entrega actualizado a '{dto.Estado}'."
        });
    }
}

public class AsignarEntregaDto
{
    public long? RepartidorId { get; set; }
}

public class CambiarEstadoEntregaDto
{
    public EstadoEntrega Estado { get; set; }
}
