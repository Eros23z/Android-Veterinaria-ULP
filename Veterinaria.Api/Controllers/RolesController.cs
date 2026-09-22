using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class RolesController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public RolesController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
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
}
