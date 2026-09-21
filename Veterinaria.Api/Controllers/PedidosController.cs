using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Common;
using Veterinaria.Api.Domain.Entities;
using Veterinaria.Api.Domain.Enums;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public PedidosController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPedidos(
        [FromQuery] string? busqueda,
        [FromQuery] EstadoPedido? estado,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        var paginaActual = pagina.GetValueOrDefault(1);
        var tamanoActual = tamano.GetValueOrDefault(10);

        if (paginaActual < 1) paginaActual = 1;
        if (tamanoActual < 1) tamanoActual = 10;

        var query = _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Items)
            .ThenInclude(i => i.Producto)
            .AsQueryable();

        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(p => (p.Notas != null && p.Notas.Contains(busqueda)) || (p.Cliente != null && p.Cliente.Nombre.Contains(busqueda)));
        }
        
        var total = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(p => p.FechaPedido)
            .Skip((paginaActual - 1) * tamanoActual)
            .Take(tamanoActual)
            .ToListAsync();

        var hayMas = ((paginaActual - 1) * tamanoActual) + items.Count < total;

        var paginaResponse = new
        {
            pagina = paginaActual,
            tamano = tamanoActual,
            total,
            hay_mas = hayMas
        };

        return Ok(new
        {
            pedidos = items,
            items = items,
            pagina = paginaResponse
        });
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(long id, [FromBody] CambiarEstadoDto dto)
    {
        var pedido = await _context.Pedidos.FindAsync(id);
        if (pedido == null) return NotFound();

        pedido.Estado = dto.Estado;
        pedido.ActualizadoEn = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CambiarEstadoDto
{
    public EstadoPedido Estado { get; set; }
}
