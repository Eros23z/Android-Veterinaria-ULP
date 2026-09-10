namespace Veterinaria.Api.Domain.Entities;

public class Producto : EntityBase
{
    public long CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Disponible { get; set; } = true;
}
