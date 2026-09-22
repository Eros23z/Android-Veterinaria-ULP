using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Auth;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SesionController : ControllerBase
{
    private readonly VeterinariaDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public SesionController(
        VeterinariaDbContext context,
        TokenService tokenService,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new
            {
                codigo = "credenciales_invalidas",
                mensaje = "Email y contraseña son obligatorios."
            });
        }

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

        if (usuario == null)
        {
            return BadRequest(new
            {
                codigo = "credenciales_invalidas",
                mensaje = "Email o contraseña incorrectos."
            });
        }

        if (!usuario.Activo)
        {
            return BadRequest(new
            {
                codigo = "usuario_inactivo",
                mensaje = "El usuario está dado de baja o inactivo."
            });
        }

        var verificacion = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            return BadRequest(new
            {
                codigo = "credenciales_invalidas",
                mensaje = "Email o contraseña incorrectos."
            });
        }

        var (accessToken, expiraEn) = _tokenService.GenerarAccessToken(usuario);
        var (refreshToken, tokenHash, refreshExpiraEn) = _tokenService.GenerarRefreshToken();

        var nuevoRefreshToken = new RefreshToken
        {
            UsuarioId = usuario.Id,
            TokenHash = tokenHash,
            ExpiraEn = refreshExpiraEn,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow,
            Activo = true
        };

        _context.RefreshTokens.Add(nuevoRefreshToken);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            token = accessToken,
            expira_en = expiraEn,
            refresh_token = refreshToken,
            usuario = new
            {
                id = usuario.Id,
                email = usuario.Email,
                rol_id = usuario.RolId,
                rol_codigo = usuario.Rol?.Codigo,
                rol_nombre = usuario.Rol?.Nombre
            }
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return Unauthorized(new
            {
                codigo = "refresh_invalido",
                mensaje = "El refresh token es obligatorio."
            });
        }

        var hash = TokenService.CalcularHashSha256(dto.RefreshToken.Trim());

        var tokenExistente = await _context.RefreshTokens
            .Include(r => r.Usuario)
            .ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (tokenExistente == null || !tokenExistente.EsActivo || !tokenExistente.Usuario.Activo)
        {
            return Unauthorized(new
            {
                codigo = "refresh_invalido",
                mensaje = "Sesión expirada o inválida."
            });
        }

        // Rotación de token: revocar el anterior
        tokenExistente.RevocadoEn = DateTimeOffset.UtcNow;
        tokenExistente.ActualizadoEn = DateTime.UtcNow;

        var (nuevoAccessToken, expiraEn) = _tokenService.GenerarAccessToken(tokenExistente.Usuario);
        var (nuevoRefreshToken, nuevoHash, refreshExpiraEn) = _tokenService.GenerarRefreshToken();

        var nuevoTokenEntidad = new RefreshToken
        {
            UsuarioId = tokenExistente.Usuario.Id,
            TokenHash = nuevoHash,
            ExpiraEn = refreshExpiraEn,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow,
            Activo = true
        };

        _context.RefreshTokens.Add(nuevoTokenEntidad);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            token = nuevoAccessToken,
            expira_en = expiraEn,
            refresh_token = nuevoRefreshToken,
            usuario = new
            {
                id = tokenExistente.Usuario.Id,
                email = tokenExistente.Usuario.Email,
                rol_id = tokenExistente.Usuario.RolId,
                rol_codigo = tokenExistente.Usuario.Rol?.Codigo,
                rol_nombre = tokenExistente.Usuario.Rol?.Nombre
            }
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            var hash = TokenService.CalcularHashSha256(dto.RefreshToken.Trim());
            var tokenExistente = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.TokenHash == hash && r.RevocadoEn == null);

            if (tokenExistente != null)
            {
                tokenExistente.RevocadoEn = DateTimeOffset.UtcNow;
                tokenExistente.ActualizadoEn = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    [HttpGet("yo")]
    [Authorize]
    public async Task<IActionResult> Yo()
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!long.TryParse(subClaim, out var usuarioId))
        {
            return Unauthorized();
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo);

        if (usuario == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            id = usuario.Id,
            email = usuario.Email,
            rol_id = usuario.RolId,
            rol_codigo = usuario.Rol?.Codigo,
            rol_nombre = usuario.Rol?.Nombre
        });
    }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshDto
{
    public string? Token { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutDto
{
    public string? RefreshToken { get; set; }
}
