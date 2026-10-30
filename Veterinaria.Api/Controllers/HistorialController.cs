using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Auth;
using Veterinaria.Api.Data;

namespace Veterinaria.Api.Controllers;

[Authorize(Roles = "ADMIN,VETERINARIO")]
[ApiController]
[Route("api/[controller]")]
public class HistorialController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public HistorialController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ListarHistorial(
        [FromQuery] string? tipo,
        [FromQuery] string? busqueda,
        [FromQuery] bool? solo_mios,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano,
        CancellationToken cancellationToken = default)
    {
        var paginaActual = Math.Max(1, pagina.GetValueOrDefault(1));
        var tamanoActual = Math.Max(1, Math.Min(50, tamano.GetValueOrDefault(15)));

        var usuarioIdActual = User.UsuarioId();
        var esAdmin = User.IsInRole("ADMIN");

        var consultaBase = _context.Historiales
            .AsNoTracking()
            .Include(h => h.Usuario)
            .Include(h => h.Pedido)
            .Include(h => h.Entrega)
            .Where(h => h.Activo);

        // Si no es ADMIN o pide solo_mios, forzar filtro por su usuarioId
        if (!esAdmin || solo_mios == true)
        {
            if (usuarioIdActual.HasValue)
            {
                consultaBase = consultaBase.Where(h => h.UsuarioId == usuarioIdActual.Value);
            }
        }

        // Búsqueda por texto (código de pedido, nombre de usuario u observación)
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var b = busqueda.Trim();
            var bNumero = b.StartsWith("PED-", StringComparison.OrdinalIgnoreCase) ? b[4..].TrimStart('0') : b;

            consultaBase = consultaBase.Where(h =>
                (h.Observacion != null && h.Observacion.Contains(b)) ||
                (h.Usuario.Email != null && h.Usuario.Email.Contains(b)) ||
                (h.PedidoId != null && h.PedidoId.Value.ToString().Contains(b)) ||
                (bNumero.Length > 0 && h.PedidoId != null && h.PedidoId.Value.ToString().Contains(bNumero)) ||
                (h.Entrega != null && h.Entrega.PedidoId.ToString().Contains(bNumero)));
        }

        // Conteos de resumen sobre la consulta base (antes del filtro de tipo)
        var totalPedidos = await consultaBase.CountAsync(h => h.PedidoId != null, cancellationToken);
        var totalEntregas = await consultaBase.CountAsync(h => h.EntregaId != null, cancellationToken);
        var totalTodos = await consultaBase.CountAsync(cancellationToken);

        var resumen = new
        {
            todos = totalTodos,
            pedidos = totalPedidos,
            entregas = totalEntregas
        };

        // Filtro por tipo
        var consultaFiltrada = consultaBase;
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var t = tipo.Trim().ToLowerInvariant();
            if (t == "pedido" || t == "pedidos")
            {
                consultaFiltrada = consultaFiltrada.Where(h => h.PedidoId != null);
            }
            else if (t == "entrega" || t == "entregas")
            {
                consultaFiltrada = consultaFiltrada.Where(h => h.EntregaId != null);
            }
        }

        var total = await consultaFiltrada.CountAsync(cancellationToken);

        var items = await consultaFiltrada
            .OrderByDescending(h => h.CreadoEn)
            .ThenByDescending(h => h.Id)
            .Skip((paginaActual - 1) * tamanoActual)
            .Take(tamanoActual)
            .Select(h => new
            {
                id = h.Id,
                tipo = h.PedidoId != null ? "pedido" : "entrega",
                referencia_id = h.PedidoId ?? h.EntregaId,
                codigo_pedido = h.PedidoId != null
                    ? $"PED-{h.PedidoId.Value:D4}"
                    : (h.Entrega != null ? $"PED-{h.Entrega.PedidoId:D4}" : null),
                usuario_id = h.UsuarioId,
                usuario_nombre = h.Usuario.Email,
                estado_anterior = h.EstadoAnterior,
                estado_nuevo = h.EstadoNuevo,
                observacion = h.Observacion,
                creado_en = h.CreadoEn
            })
            .ToListAsync(cancellationToken);

        var hayMas = ((paginaActual - 1) * tamanoActual) + items.Count < total;

        return Ok(new
        {
            historial = items,
            resumen,
            paginacion = new
            {
                pagina = paginaActual,
                tamano = tamanoActual,
                total,
                hay_mas = hayMas
            }
        });
    }
}
