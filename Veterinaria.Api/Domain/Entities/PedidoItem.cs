using System.Text.Json.Serialization;
using Veterinaria.Api.Domain.Enums;

namespace Veterinaria.Api.Domain.Entities;

public class PedidoItem : EntityBase
{
    public long PedidoId { get; set; }
    [JsonIgnore]
    public Pedido? Pedido { get; set; }
    
    public long ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public EstadoItemPedido Estado { get; set; } = EstadoItemPedido.Pendiente;
}
