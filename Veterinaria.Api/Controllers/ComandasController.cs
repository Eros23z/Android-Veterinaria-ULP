using System.Security.Claims;
using System.Text.Json;
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
public class ComandasController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public ComandasController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ListarComandas(CancellationToken cancellationToken = default)
    {
        var estadosPreparacion = new[] { EstadoPedido.Confirmado, EstadoPedido.EnPreparacion, EstadoPedido.Listo };

        var pedidos = await _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Items)
                .ThenInclude(i => i.Producto)
            .Where(p => p.Activo && estadosPreparacion.Contains(p.Estado))
            .OrderBy(p => p.FechaPedido)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

        var resultado = pedidos.Select(p => new
        {
            id = p.Id,
            codigo = $"PED-{p.Id:D4}",
            cliente_id = p.ClienteId,
            cliente_nombre = p.Cliente?.Nombre ?? "Cliente general",
            tipo = p.Tipo,
            estado = FormatearEnum(p.Estado),
            fecha_pedido = p.FechaPedido,
            creado_en = p.CreadoEn,
            notas = p.Notas,
            total = p.Total,
            items_listos = p.Items.Count(i => i.Activo && i.Estado == EstadoItemPedido.Listo),
            items_totales = p.Items.Count(i => i.Activo),
            items = p.Items.Where(i => i.Activo).Select(i => new
            {
                id = i.Id,
                producto_id = i.ProductoId,
                producto_nombre = i.Producto?.Nombre ?? "Producto",
                cantidad = i.Cantidad,
                precio_unitario = i.PrecioUnitario,
                subtotal = i.Subtotal,
                estado = FormatearEnum(i.Estado)
            }).ToList()
        });

        return Ok(new { comandas = resultado });
    }

    [HttpPut("items/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoItem(long id, [FromBody] CambiarEstadoItemDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Estado))
        {
            return BadRequest(new { codigo = "estado_requerido", mensaje = "Debe especificar el nuevo estado del ítem." });
        }

        if (!TryParseEstadoItem(dto.Estado, out var nuevoEstadoItem))
        {
            return BadRequest(new { codigo = "estado_invalido", mensaje = "El estado del ítem no es válido." });
        }

        var item = await _context.PedidoItems
            .Include(pi => pi.Pedido)
            .FirstOrDefaultAsync(pi => pi.Id == id && pi.Activo, cancellationToken);

        if (item == null || item.Pedido == null)
        {
            return NotFound(new { codigo = "item_no_encontrado", mensaje = "El ítem no fue encontrado." });
        }

        var pedido = item.Pedido;
        var estadosPermitidos = new[] { EstadoPedido.Confirmado, EstadoPedido.EnPreparacion, EstadoPedido.Listo };
        if (!estadosPermitidos.Contains(pedido.Estado))
        {
            return BadRequest(new
            {
                codigo = "pedido_no_editable",
                mensaje = $"No se pueden modificar ítems de un pedido en estado '{pedido.Estado}'."
            });
        }

        item.Estado = nuevoEstadoItem;
        item.ActualizadoEn = DateTime.UtcNow;

        await RecalcularEstadoPedidoAsync(pedido, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            item_id = item.Id,
            item_estado = FormatearEnum(item.Estado),
            pedido_id = pedido.Id,
            pedido_estado = FormatearEnum(pedido.Estado),
            mensaje = "Estado de comanda actualizado."
        });
    }

    [HttpPut("{id}/listo")]
    public async Task<IActionResult> MarcarPedidoListo(long id, CancellationToken cancellationToken = default)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.Activo, cancellationToken);

        if (pedido == null)
        {
            return NotFound(new { codigo = "pedido_no_encontrado", mensaje = "El pedido no fue encontrado." });
        }

        var estadosPermitidos = new[] { EstadoPedido.Confirmado, EstadoPedido.EnPreparacion, EstadoPedido.Listo };
        if (!estadosPermitidos.Contains(pedido.Estado))
        {
            return BadRequest(new
            {
                codigo = "pedido_no_editable",
                mensaje = $"No se puede marcar listo un pedido en estado '{pedido.Estado}'."
            });
        }

        foreach (var item in pedido.Items.Where(i => i.Activo))
        {
            item.Estado = EstadoItemPedido.Listo;
            item.ActualizadoEn = DateTime.UtcNow;
        }

        await RecalcularEstadoPedidoAsync(pedido, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = pedido.Id,
            estado = FormatearEnum(pedido.Estado),
            mensaje = "Todos los ítems del pedido fueron marcados como listos."
        });
    }

    private async Task RecalcularEstadoPedidoAsync(Pedido pedido, CancellationToken ct)
    {
        // Obligatorio: consultar entidades completas para que el Change Tracker de EF use los valores modificados en memoria
        var items = await _context.PedidosItems
            .Where(x => x.PedidoId == pedido.Id && x.Activo)
            .ToListAsync(ct);

        if (items.Count == 0) return;

        EstadoPedido nuevoEstado;
        if (items.All(x => x.Estado == EstadoItemPedido.Listo))
        {
            nuevoEstado = EstadoPedido.Listo;
        }
        else if (items.All(x => x.Estado == EstadoItemPedido.Pendiente))
        {
            nuevoEstado = EstadoPedido.Confirmado;
        }
        else
        {
            nuevoEstado = EstadoPedido.EnPreparacion;
        }

        if (nuevoEstado != pedido.Estado)
        {
            var usuarioActualId = User.UsuarioId() ?? 1;

            _context.RegistrarPedido(
                usuarioActualId,
                pedido.Id,
                pedido.Estado.ToString(),
                nuevoEstado.ToString(),
                "Movimiento de preparación.");

            // Si pasa a Listo y el creador es distinto al usuario actual, notificar
            if (nuevoEstado == EstadoPedido.Listo &&
                pedido.UsuarioCreadorId.HasValue &&
                pedido.UsuarioCreadorId.Value != usuarioActualId)
            {
                var nombreEjecutor = User.UsuarioEmail()
                    ?? User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value
                    ?? "Área de preparación";

                _context.Notificaciones.Add(new Notificacion
                {
                    UsuarioId = pedido.UsuarioCreadorId.Value,
                    Titulo = $"Pedido PED-{pedido.Id:D4} Listo",
                    Mensaje = $"{nombreEjecutor} completó la preparación del pedido PED-{pedido.Id:D4}.",
                    Tipo = TipoNotificacion.Pedido,
                    Activo = true,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                });
            }

            pedido.Estado = nuevoEstado;
            pedido.ActualizadoEn = DateTime.UtcNow;
        }
    }

    private static bool TryParseEstadoItem(string valor, out EstadoItemPedido estado)
    {
        var limpio = valor.Replace("_", "").Trim();
        return Enum.TryParse<EstadoItemPedido>(limpio, ignoreCase: true, out estado);
    }

    private static string FormatearEnum<T>(T valor) where T : struct, Enum
    {
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(valor.ToString());
    }
}

public class CambiarEstadoItemDto
{
    public string Estado { get; set; } = string.Empty;
}
