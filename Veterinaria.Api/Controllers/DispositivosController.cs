using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Auth;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,VETERINARIO")]
public class DispositivosController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public DispositivosController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpPut]
    public async Task<IActionResult> Registrar([FromBody] RegistrarDispositivoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            return BadRequest(new
            {
                codigo = "token_requerido",
                mensaje = "El token push del dispositivo es obligatorio."
            });
        }

        var usuarioId = User.UsuarioId();
        if (!usuarioId.HasValue)
        {
            return Unauthorized();
        }

        var tokenLimpiado = dto.Token.Trim();
        var dispositivo = await _context.DispositivosPush
            .FirstOrDefaultAsync(d => d.Token == tokenLimpiado);

        if (dispositivo != null)
        {
            dispositivo.UsuarioId = usuarioId.Value;
            dispositivo.ActualizadoEn = DateTime.UtcNow;
            dispositivo.Activo = true;
        }
        else
        {
            dispositivo = new DispositivoPush
            {
                Token = tokenLimpiado,
                UsuarioId = usuarioId.Value,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow,
                Activo = true
            };
            _context.DispositivosPush.Add(dispositivo);
        }

        await _context.SaveChangesAsync();

        return Ok(new { registrado = true });
    }
}

public class RegistrarDispositivoDto
{
    public string Token { get; set; } = string.Empty;
}
