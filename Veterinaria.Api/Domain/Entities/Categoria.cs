namespace Veterinaria.Api.Domain.Entities;

public class Categoria : EntityBase
{
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
