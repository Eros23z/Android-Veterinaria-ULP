using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Dtos;

namespace Veterinaria.Api.Controllers;

[Authorize(Roles = "ADMIN,VETERINARIO")]
[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public ProductosController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductoDto>> GetById(long id)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.Id == id && p.Activo)
            .Select(p => new ProductoDto(
                p.Id,
                p.CategoriaId,
                p.Categoria != null ? p.Categoria.Nombre : string.Empty,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.ImagenUrl,
                p.Disponible
            ))
            .FirstOrDefaultAsync();

        if (producto == null)
        {
            return NotFound(new { mensaje = $"Producto con id {id} no encontrado" });
        }

        return Ok(producto);
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ProductosResumenDto>> GetResumen()
    {
        var categorias = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Orden)
            .ThenBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.Id, c.Nombre, c.Orden))
            .ToListAsync();

        var productos = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.Activo)
            .OrderBy(p => p.Categoria != null ? p.Categoria.Orden : 0)
            .ThenBy(p => p.Nombre)
            .Select(p => new ProductoDto(
                p.Id,
                p.CategoriaId,
                p.Categoria != null ? p.Categoria.Nombre : string.Empty,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.ImagenUrl,
                p.Disponible
            ))
            .ToListAsync();

        var total = productos.Count;
        var disponibles = productos.Count(p => p.Disponible);
        var noDisponibles = total - disponibles;

        var resumenContadores = new ResumenContadoresDto(total, disponibles, noDisponibles);

        var respuesta = new ProductosResumenDto(resumenContadores, categorias, productos);
        return Ok(respuesta);
    }
}
