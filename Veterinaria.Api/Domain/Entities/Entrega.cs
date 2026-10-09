using Veterinaria.Api.Domain.Enums;

namespace Veterinaria.Api.Domain.Entities;

public class Entrega : EntityBase
{
    public long PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public long? RepartidorId { get; set; }
    public Usuario? Repartidor { get; set; }

    public string DireccionLinea { get; set; } = string.Empty;
    public string? DireccionReferencia { get; set; }

    public decimal? DireccionLatitud { get; set; }
    public decimal? DireccionLongitud { get; set; }

    public EstadoEntrega Estado { get; set; } = EstadoEntrega.Pendiente;

    public DateTimeOffset? AsignadaEn { get; set; }
    public DateTimeOffset? EntregadaEn { get; set; }
    public string? Observaciones { get; set; }
}
