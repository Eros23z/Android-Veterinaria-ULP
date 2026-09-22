using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Auth;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string token, DateTime expiraEn) GenerarAccessToken(Usuario usuario)
    {
        var secretKey = _configuration["Jwt:Key"] ?? "ClaveEfimeraVeterinariaSanRoque2026SuperSeguraDesarrollo12345!";
        var issuer = _configuration["Jwt:Issuer"] ?? "Veterinaria.Api";
        var audience = _configuration["Jwt:Audience"] ?? "Veterinaria.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (usuario.Rol != null && !string.IsNullOrWhiteSpace(usuario.Rol.Codigo))
        {
            claims.Add(new Claim("rol_codigo", usuario.Rol.Codigo));
            claims.Add(new Claim(ClaimTypes.Role, usuario.Rol.Codigo));
        }

        var expiraEn = DateTime.UtcNow.AddHours(8);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiraEn,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiraEn);
    }

    public (string refreshToken, string tokenHash, DateTimeOffset expiraEn) GenerarRefreshToken()
    {
        var randomBytes = new byte[48];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);
        var tokenHash = CalcularHashSha256(refreshToken);
        var expiraEn = DateTimeOffset.UtcNow.AddDays(7);

        return (refreshToken, tokenHash, expiraEn);
    }

    public static string CalcularHashSha256(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
