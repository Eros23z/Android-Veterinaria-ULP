using System.Text.RegularExpressions;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;

#pragma warning disable CS0618

namespace Veterinaria.Api.Push;

public class EnvioPush
{
    private readonly ILogger<EnvioPush> _logger;
    private readonly bool _firebaseHabilitado;
    private static readonly Regex RegexCodigoPedido = new(@"PED-[0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public EnvioPush(IConfiguration configuration, ILogger<EnvioPush> logger)
    {
        _logger = logger;

        var rutaCredencial = configuration["Firebase:CuentaDeServicio"];
        if (string.IsNullOrWhiteSpace(rutaCredencial))
        {
            _logger.LogInformation("Firebase Push: 'Firebase:CuentaDeServicio' no configurado. Modo resiliente sin push activo.");
            _firebaseHabilitado = false;
            return;
        }

        if (!File.Exists(rutaCredencial))
        {
            _logger.LogInformation("Firebase Push: No se encontró el archivo de credenciales en '{Ruta}'. Modo resiliente sin push activo.", rutaCredencial);
            _firebaseHabilitado = false;
            return;
        }

        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(rutaCredencial)
                });
            }
            _firebaseHabilitado = true;
            _logger.LogInformation("Firebase Push inicializado exitosamente desde '{Ruta}'.", rutaCredencial);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al inicializar FirebaseApp. Modo resiliente sin push activo.");
            _firebaseHabilitado = false;
        }
    }

    public async Task EnviarAsync(VeterinariaDbContext db, IReadOnlyCollection<Notificacion> notificaciones, CancellationToken ct = default)
    {
        if (!_firebaseHabilitado || notificaciones == null || notificaciones.Count == 0)
        {
            return;
        }

        var usuariosIds = notificaciones.Select(n => n.UsuarioId).Distinct().ToList();
        if (usuariosIds.Count == 0)
        {
            return;
        }

        var dispositivos = await db.DispositivosPush
            .Where(d => d.Activo && usuariosIds.Contains(d.UsuarioId))
            .ToListAsync(ct);

        if (dispositivos.Count == 0)
        {
            return;
        }

        var mensajes = new List<Message>();
        var tokensMap = new List<string>();

        foreach (var notif in notificaciones)
        {
            var match = RegexCodigoPedido.Match(notif.Titulo);
            var codigoPedido = match.Success ? match.Value.ToUpperInvariant() : null;

            var dispositivosDestinatario = dispositivos.Where(d => d.UsuarioId == notif.UsuarioId);

            foreach (var disp in dispositivosDestinatario)
            {
                var data = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(codigoPedido))
                {
                    data["pedido_codigo"] = codigoPedido;
                }

                var msg = new Message
                {
                    Token = disp.Token,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = notif.Titulo,
                        Body = notif.Mensaje
                    },
                    Data = data.Count > 0 ? data : null,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            ChannelId = "avisos"
                        }
                    }
                };

                mensajes.Add(msg);
                tokensMap.Add(disp.Token);
            }
        }

        if (mensajes.Count == 0)
        {
            return;
        }

        try
        {
            var batchResponse = await FirebaseMessaging.DefaultInstance.SendEachAsync(mensajes, ct);

            var tokensInvalidos = new HashSet<string>();
            for (int i = 0; i < batchResponse.Responses.Count; i++)
            {
                var resp = batchResponse.Responses[i];
                if (!resp.IsSuccess && resp.Exception != null)
                {
                    var codigoError = resp.Exception.MessagingErrorCode;
                    if (codigoError == MessagingErrorCode.Unregistered || codigoError == MessagingErrorCode.InvalidArgument)
                    {
                        tokensInvalidos.Add(tokensMap[i]);
                    }
                }
            }

            if (tokensInvalidos.Count > 0)
            {
                var dispositivosVencidos = dispositivos.Where(d => tokensInvalidos.Contains(d.Token)).ToList();
                if (dispositivosVencidos.Count > 0)
                {
                    db.DispositivosPush.RemoveRange(dispositivosVencidos);
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Se removieron {Cantidad} tokens push vencidos de la base de datos.", dispositivosVencidos.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo inesperado al enviar notificaciones push a través de FirebaseMessaging.");
        }
    }
}
