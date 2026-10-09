using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Veterinaria.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static long? UsuarioId(this ClaimsPrincipal principal)
    {
        var val = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return long.TryParse(val, out var id) ? id : null;
    }

    public static string? UsuarioEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
    }
}
