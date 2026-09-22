using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class UsuariosController : ControllerBase
{
    private readonly VeterinariaDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuariosController(
        VeterinariaDbContext context,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsuarios(
        [FromQuery] string? busqueda,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        var paginaActual = pagina.GetValueOrDefault(1);
        var tamanoActual = tamano.GetValueOrDefault(10);

        if (paginaActual < 1) paginaActual = 1;
        if (tamanoActual < 1) tamanoActual = 10;

        var consulta = _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Activo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var b = busqueda.Trim().ToLower();
            consulta = consulta.Where(u => u.Email.ToLower().Contains(b));
        }

        var total = await consulta.CountAsync();

        var items = await consulta
            .OrderByDescending(u => u.CreadoEn)
            .Skip((paginaActual - 1) * tamanoActual)
            .Take(tamanoActual)
            .Select(u => new
            {
                id = u.Id,
                email = u.Email,
                rol_id = u.RolId,
                rol_codigo = u.Rol != null ? u.Rol.Codigo : null,
                rol_nombre = u.Rol != null ? u.Rol.Nombre : "Sin rol",
                creado_en = u.CreadoEn,
                activo = u.Activo
            })
            .ToListAsync();

        var hayMas = ((paginaActual - 1) * tamanoActual) + items.Count < total;

        var paginaResponse = new
        {
            pagina = paginaActual,
            tamano = tamanoActual,
            total,
            hay_mas = hayMas
        };

        return Ok(new { usuarios = items, pagina = paginaResponse });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .Where(r => r.Activo)
            .OrderBy(r => r.Nombre)
            .Select(r => new
            {
                id = r.Id,
                nombre = r.Nombre,
                codigo = r.Codigo
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new
            {
                codigo = "datos_requeridos",
                mensaje = "El email y la contraseña son obligatorios."
            });
        }

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();

        var existe = await _context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == emailNormalizado);

        if (existe)
        {
            return BadRequest(new
            {
                codigo = "usuario_duplicado",
                mensaje = "Ya existe un usuario registrado con ese email."
            });
        }

        if (dto.RolId.HasValue)
        {
            var existeRol = await _context.Roles.AnyAsync(r => r.Id == dto.RolId.Value && r.Activo);
            if (!existeRol)
            {
                return BadRequest(new
                {
                    codigo = "rol_invalido",
                    mensaje = "El rol especificado no existe o no está activo."
                });
            }
        }

        var nuevoUsuario = new Usuario
        {
            Email = emailNormalizado,
            RolId = dto.RolId,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow,
            Activo = true
        };

        nuevoUsuario.PasswordHash = _passwordHasher.HashPassword(nuevoUsuario, dto.Password);

        _context.Usuarios.Add(nuevoUsuario);
        await _context.SaveChangesAsync();

        var rol = dto.RolId.HasValue
            ? await _context.Roles.FindAsync(dto.RolId.Value)
            : null;

        return CreatedAtAction(nameof(GetUsuarios), new { id = nuevoUsuario.Id }, new
        {
            id = nuevoUsuario.Id,
            email = nuevoUsuario.Email,
            rol_id = nuevoUsuario.RolId,
            rol_codigo = rol?.Codigo,
            rol_nombre = rol?.Nombre ?? "Sin rol",
            creado_en = nuevoUsuario.CreadoEn,
            activo = nuevoUsuario.Activo
        });
    }

    [HttpPut("{id}/rol")]
    public async Task<IActionResult> ActualizarRol(long id, [FromBody] ActualizarRolDto dto)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == id && u.Activo);

        if (usuario == null)
        {
            return NotFound(new
            {
                codigo = "usuario_no_encontrado",
                mensaje = "Usuario no encontrado."
            });
        }

        if (dto.RolId.HasValue)
        {
            var existeRol = await _context.Roles.AnyAsync(r => r.Id == dto.RolId.Value && r.Activo);
            if (!existeRol)
            {
                return BadRequest(new
                {
                    codigo = "rol_invalido",
                    mensaje = "El rol especificado no existe."
                });
            }
        }

        usuario.RolId = dto.RolId;
        usuario.ActualizadoEn = DateTime.UtcNow;

        // Revocar todos los refresh tokens activos para forzar revalidación inmediata
        var refreshTokens = await _context.RefreshTokens
            .Where(r => r.UsuarioId == id && r.RevocadoEn == null)
            .ToListAsync();

        foreach (var rt in refreshTokens)
        {
            rt.RevocadoEn = DateTimeOffset.UtcNow;
            rt.ActualizadoEn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var rol = dto.RolId.HasValue
            ? await _context.Roles.FindAsync(dto.RolId.Value)
            : null;

        return Ok(new
        {
            id = usuario.Id,
            email = usuario.Email,
            rol_id = usuario.RolId,
            rol_codigo = rol?.Codigo,
            rol_nombre = rol?.Nombre ?? "Sin rol"
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarUsuario(long id)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id && u.Activo);

        if (usuario == null)
        {
            return NotFound(new
            {
                codigo = "usuario_no_encontrado",
                mensaje = "Usuario no encontrado."
            });
        }

        usuario.Activo = false;
        usuario.ActualizadoEn = DateTime.UtcNow;

        // Revocar todos sus refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(r => r.UsuarioId == id && r.RevocadoEn == null)
            .ToListAsync();

        foreach (var rt in refreshTokens)
        {
            rt.RevocadoEn = DateTimeOffset.UtcNow;
            rt.ActualizadoEn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CrearUsuarioDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public long? RolId { get; set; }
}

public class ActualizarRolDto
{
    public long? RolId { get; set; }
}
