namespace Veterinaria.Api.Domain.Common;

public class Paginacion<T>
{
    public int Pagina { get; set; }
    public int Tamano { get; set; }
    public int Total { get; set; }
    public bool HayMas { get; set; }
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
}
