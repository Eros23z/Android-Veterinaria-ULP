using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Data;

public class VeterinariaDbContext : DbContext
{
    public VeterinariaDbContext(DbContextOptions<VeterinariaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItems => Set<PedidoItem>();
    public DbSet<Entrega> Entregas => Set<Entrega>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Orden).HasDefaultValue(0);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Descripcion).HasMaxLength(500);
            entity.Property(p => p.Precio).HasPrecision(18, 2);
            entity.Property(p => p.ImagenUrl).HasMaxLength(300);
            entity.Property(p => p.Disponible).HasDefaultValue(true);

            entity.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Email).HasMaxLength(150);
            entity.Property(c => c.Telefono).HasMaxLength(50);
            entity.Property(c => c.Direccion).HasMaxLength(500);
            entity.Property(c => c.DireccionLatitud).HasPrecision(18, 6);
            entity.Property(c => c.DireccionLongitud).HasPrecision(18, 6);
            entity.Property(c => c.FotoUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Total).HasPrecision(18, 2);
            entity.Property(p => p.Tipo).IsRequired().HasMaxLength(50).HasDefaultValue("mostrador");
            entity.Property(p => p.Notas).HasMaxLength(1000);

            entity.HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.UsuarioCreador)
                .WithMany()
                .HasForeignKey(p => p.UsuarioCreadorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PedidoItem>(entity =>
        {
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(pi => pi.Subtotal).HasPrecision(18, 2);

            entity.HasOne(pi => pi.Pedido)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pi => pi.Producto)
                .WithMany()
                .HasForeignKey(pi => pi.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Codigo).IsRequired().HasMaxLength(50);
            entity.HasIndex(r => r.Codigo).IsUnique();
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(64);
            entity.HasIndex(rt => rt.TokenHash).IsUnique();

            entity.HasOne(rt => rt.Usuario)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Entrega>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DireccionLinea).IsRequired().HasMaxLength(300);
            entity.Property(e => e.DireccionReferencia).HasMaxLength(300);
            entity.Property(e => e.DireccionLatitud).HasPrecision(18, 6);
            entity.Property(e => e.DireccionLongitud).HasPrecision(18, 6);
            entity.Property(e => e.Estado).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Observaciones).HasMaxLength(500);

            entity.HasOne(e => e.Pedido)
                .WithMany()
                .HasForeignKey(e => e.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Repartidor)
                .WithMany()
                .HasForeignKey(e => e.RepartidorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Mensaje).IsRequired().HasMaxLength(1000);
            entity.Property(n => n.Tipo).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(n => n.Usuario)
                .WithMany()
                .HasForeignKey(n => n.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
