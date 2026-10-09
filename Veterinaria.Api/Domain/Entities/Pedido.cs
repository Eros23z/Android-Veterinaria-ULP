using Veterinaria.Api.Domain.Enums;

namespace Veterinaria.Api.Domain.Entities;

public class Pedido : EntityBase
{
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    
    public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public EstadoPedido Estado { get; set; } = EstadoPedido.Borrador;
    public string? Notas { get; set; }

    public string Tipo { get; set; } = "mostrador";
    public long? UsuarioCreadorId { get; set; }
    public Usuario? UsuarioCreador { get; set; }

    public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
}
