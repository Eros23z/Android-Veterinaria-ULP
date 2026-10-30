namespace Veterinaria.Api.Domain.Entities;

public class Historial : EntityBase
{
    public long? PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public long? EntregaId { get; set; }
    public Entrega? Entrega { get; set; }

    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Observacion { get; set; }
}
