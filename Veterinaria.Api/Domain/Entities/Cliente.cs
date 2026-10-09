namespace Veterinaria.Api.Domain.Entities;

public class Cliente : EntityBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public decimal? DireccionLatitud { get; set; }
    public decimal? DireccionLongitud { get; set; }
    public string? FotoUrl { get; set; }
}
