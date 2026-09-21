using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public ClientesController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetClientes(
        [FromQuery] string? busqueda,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        var paginaActual = pagina.GetValueOrDefault(1);
        var tamanoActual = tamano.GetValueOrDefault(10);

        if (paginaActual < 1) paginaActual = 1;
        if (tamanoActual < 1) tamanoActual = 10;

        var consulta = _context.Clientes
            .Where(x => x.Activo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            consulta = consulta.Where(x => x.Nombre.Contains(busqueda) || x.Telefono.Contains(busqueda));
        }

        var total = await consulta.CountAsync();

        var items = await consulta
            .OrderByDescending(c => c.CreadoEn)
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

        return Ok(new { clientes = items, pagina = paginaResponse });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCliente(long id)
    {
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.Activo);
        if (cliente == null)
        {
            return NotFound();
        }
        return Ok(cliente);
    }

    [HttpPost]
    public async Task<IActionResult> CrearCliente([FromBody] Cliente cliente)
    {
        if (string.IsNullOrWhiteSpace(cliente.Nombre) || string.IsNullOrWhiteSpace(cliente.Telefono))
        {
            return BadRequest(new
            {
                codigo = "datos_requeridos",
                mensaje = "El nombre y el teléfono son obligatorios."
            });
        }

        var existeTelefono = await _context.Clientes
            .AnyAsync(c => c.Activo && c.Telefono == cliente.Telefono);

        if (existeTelefono)
        {
            return BadRequest(new
            {
                codigo = "cliente_duplicado",
                mensaje = "Ya existe un cliente con ese teléfono."
            });
        }

        cliente.Activo = true;
        cliente.CreadoEn = DateTime.UtcNow;
        cliente.ActualizadoEn = DateTime.UtcNow;

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarCliente(long id, [FromBody] Cliente clienteDto)
    {
        if (string.IsNullOrWhiteSpace(clienteDto.Nombre) || string.IsNullOrWhiteSpace(clienteDto.Telefono))
        {
            return BadRequest(new
            {
                codigo = "datos_requeridos",
                mensaje = "El nombre y el teléfono son obligatorios."
            });
        }

        var existeTelefono = await _context.Clientes
            .AnyAsync(c => c.Id != id && c.Activo && c.Telefono == clienteDto.Telefono);

        if (existeTelefono)
        {
            return BadRequest(new
            {
                codigo = "cliente_duplicado",
                mensaje = "Ya existe un cliente con ese teléfono."
            });
        }

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.Activo);
        if (cliente == null)
        {
            return NotFound();
        }

        cliente.Nombre = clienteDto.Nombre;
        cliente.Email = clienteDto.Email ?? string.Empty;
        cliente.Telefono = clienteDto.Telefono;
        cliente.Direccion = clienteDto.Direccion ?? string.Empty;
        cliente.ActualizadoEn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(cliente);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarCliente(long id)
    {
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.Activo);
        if (cliente == null)
        {
            return NotFound();
        }

        cliente.Activo = false;
        cliente.ActualizadoEn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

