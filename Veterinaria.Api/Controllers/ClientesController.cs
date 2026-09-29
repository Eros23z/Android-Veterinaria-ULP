using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Controllers;

[Authorize(Roles = "ADMIN,VETERINARIO")]
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly VeterinariaDbContext _context;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long TamanoMaximoFotoBytes = 5 * 1024 * 1024; // 5 MB

    public ClientesController(VeterinariaDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CrearCliente([FromForm] GuardarClienteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Telefono))
        {
            return BadRequest(new
            {
                codigo = "datos_requeridos",
                mensaje = "El nombre y el teléfono son obligatorios."
            });
        }

        var existeTelefono = await _context.Clientes
            .AnyAsync(c => c.Activo && c.Telefono == request.Telefono);

        if (existeTelefono)
        {
            return BadRequest(new
            {
                codigo = "cliente_duplicado",
                mensaje = "Ya existe un cliente con ese teléfono."
            });
        }

        string? fotoUrl = null;
        if (request.Foto != null && request.Foto.Length > 0)
        {
            var validacionFoto = ValidarFoto(request.Foto);
            if (validacionFoto != null)
            {
                return validacionFoto;
            }

            fotoUrl = await GuardarArchivoFotoAsync(request.Foto);
        }

        var cliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
            Telefono = request.Telefono.Trim(),
            Direccion = request.Direccion?.Trim() ?? string.Empty,
            FotoUrl = fotoUrl,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ActualizarCliente(long id, [FromForm] GuardarClienteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Telefono))
        {
            return BadRequest(new
            {
                codigo = "datos_requeridos",
                mensaje = "El nombre y el teléfono son obligatorios."
            });
        }

        var existeTelefono = await _context.Clientes
            .AnyAsync(c => c.Id != id && c.Activo && c.Telefono == request.Telefono);

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

        // Manejo de foto: si viene foto nueva, guardar la nueva y eliminar físicamente la anterior
        if (request.Foto != null && request.Foto.Length > 0)
        {
            var validacionFoto = ValidarFoto(request.Foto);
            if (validacionFoto != null)
            {
                return validacionFoto;
            }

            var nuevaFotoUrl = await GuardarArchivoFotoAsync(request.Foto);

            // Eliminar físicamente foto anterior si pertenecía a /uploads/clientes/ (nunca de /seed/)
            EliminarFotoFisicaSiCorresponde(cliente.FotoUrl);

            cliente.FotoUrl = nuevaFotoUrl;
        }
        // Si no viene foto, conservar FotoUrl previa

        cliente.Nombre = request.Nombre.Trim();
        cliente.Email = request.Email?.Trim() ?? string.Empty;
        cliente.Telefono = request.Telefono.Trim();
        cliente.Direccion = request.Direccion?.Trim() ?? string.Empty;
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

    #region Métodos Auxiliares de Foto

    private IActionResult? ValidarFoto(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            return BadRequest(new
            {
                codigo = "formato_foto_invalido",
                mensaje = "El formato de la foto debe ser .jpg, .jpeg, .png o .webp."
            });
        }

        if (archivo.Length > TamanoMaximoFotoBytes)
        {
            return BadRequest(new
            {
                codigo = "tamano_foto_excedido",
                mensaje = "La foto no debe superar el tamaño máximo permitido de 5 MB."
            });
        }

        return null;
    }

    private async Task<string> GuardarArchivoFotoAsync(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var uploadsFolder = ObtenerCarpetaUploads();
        
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var nombreUnico = $"{Guid.NewGuid():N}{extension}";
        var rutaCompleta = Path.Combine(uploadsFolder, nombreUnico);

        await using (var fileStream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(fileStream);
        }

        return $"/uploads/clientes/{nombreUnico}";
    }

    private void EliminarFotoFisicaSiCorresponde(string? fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl)) return;

        // Solo eliminar si está en /uploads/clientes/, nunca de /seed/ u otras carpetas
        if (fotoUrl.StartsWith("/uploads/clientes/", StringComparison.OrdinalIgnoreCase))
        {
            var nombreArchivo = Path.GetFileName(fotoUrl);
            var rutaFisica = Path.Combine(ObtenerCarpetaUploads(), nombreArchivo);
            if (System.IO.File.Exists(rutaFisica))
            {
                try
                {
                    System.IO.File.Delete(rutaFisica);
                }
                catch
                {
                    // Evitar que un fallo de I/O bloquee la operación
                }
            }
        }
    }

    private string ObtenerCarpetaUploads()
    {
        var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        return Path.Combine(root, "uploads", "clientes");
    }

    #endregion
}

public class GuardarClienteRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public IFormFile? Foto { get; set; }
}
