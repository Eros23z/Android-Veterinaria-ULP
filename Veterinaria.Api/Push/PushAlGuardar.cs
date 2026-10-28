using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;

namespace Veterinaria.Api.Push;

public class PushAlGuardar : SaveChangesInterceptor
{
    private readonly EnvioPush _envioPush;
    private readonly ILogger<PushAlGuardar> _logger;
    private readonly List<Notificacion> _notificacionesCapturadas = new();

    public PushAlGuardar(EnvioPush envioPush, ILogger<PushAlGuardar> logger)
    {
        _envioPush = envioPush;
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _notificacionesCapturadas.Clear();

        if (eventData.Context is VeterinariaDbContext db)
        {
            var nuevas = db.ChangeTracker.Entries<Notificacion>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            if (nuevas.Count > 0)
            {
                _notificacionesCapturadas.AddRange(nuevas);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_notificacionesCapturadas.Count > 0 && eventData.Context is VeterinariaDbContext db)
        {
            var notificaciones = _notificacionesCapturadas.ToList();
            _notificacionesCapturadas.Clear();

            try
            {
                await _envioPush.EnviarAsync(db, notificaciones, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al disparar notificaciones push tras guardar cambios en BD.");
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
