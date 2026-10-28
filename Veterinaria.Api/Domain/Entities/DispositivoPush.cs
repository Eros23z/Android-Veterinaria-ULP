namespace Veterinaria.Api.Domain.Entities;

public class DispositivoPush : EntityBase
{
    public long UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Token { get; set; } = string.Empty;
}
