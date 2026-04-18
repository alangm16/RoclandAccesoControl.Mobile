using Android.App;
using Firebase.Messaging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace RoclandAccesoControl.Mobile.Platforms.Android;

/// <summary>
/// Servicio que recibe mensajes FCM tanto en primer plano como en segundo plano.
///
/// LÓGICA DE NOTIFICACIONES:
/// ┌─────────────────┬────────────────────────────────────────────────────────────┐
/// │ Estado de la app│ Qué pasa                                                   │
/// ├─────────────────┼────────────────────────────────────────────────────────────┤
/// │ Abierta          │ OnMessageReceived se llama → mostramos notificación local  │
/// │                 │ El guardia ve el banner Y SignalR ya actualizó la lista     │
/// ├─────────────────┼────────────────────────────────────────────────────────────┤
/// │ Minimizada      │ OnMessageReceived se llama → mostramos notificación local  │
/// │ (segundo plano) │ El tap navega directamente al detalle                      │
/// ├─────────────────┼────────────────────────────────────────────────────────────┤
/// │ Cerrada         │ FCM muestra la notificación por su cuenta (data-only no,   │
/// │                 │ pero OnMessageReceived sí se llama en un proceso separado)  │
/// │                 │ → mostramos notificación local que sí tiene ReturningData  │
/// └─────────────────┴────────────────────────────────────────────────────────────┘
/// </summary>
[Service(Exported = false, Name = "com.rocland.accesocontrol.MyFirebaseMessagingService")]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MyFirebaseMessagingService : FirebaseMessagingService
{
    public const string PrefKey = "fcm_token";

    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        Preferences.Set(PrefKey, token);
        System.Diagnostics.Debug.WriteLine($"[FCM] Nuevo token: {token[..10]}...");
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        // Extraemos la data del mensaje
        string titulo = "Nueva solicitud de acceso";
        if (message.Data.TryGetValue("title", out var titleStr) && !string.IsNullOrEmpty(titleStr))
            titulo = titleStr;

        string cuerpo = "";
        if (message.Data.TryGetValue("body", out var bodyStr))
            cuerpo = bodyStr;

        int solicitudId = 0;
        if (message.Data.TryGetValue("solicitudId", out var idStr))
            int.TryParse(idStr, out solicitudId);

        System.Diagnostics.Debug.WriteLine(
            $"[FCM] Mensaje recibido. Título='{titulo}' SolicitudId={solicitudId}");

        // ── SIEMPRE mostramos la notificación local ──────────────────────────
        // Razón: Plugin.LocalNotification es el único que puede serializar
        // correctamente el ReturningData en el Intent para que el tap navegue
        // al detalle. Si dejamos que FCM muestre la notificación por su cuenta
        // (con el nodo "notification"), el tap solo abre la app pero sin datos.
        //
        // Cuando la app está en PRIMER PLANO, Android bloqueará el banner del
        // sistema pero nosotros podemos manejar la UI con SignalR. Aun así
        // enviamos la notificación local para que aparezca el badge/banner
        // en caso de que el usuario esté en otra pantalla dentro de la misma app.
        MostrarNotificacionLocal(solicitudId, titulo, cuerpo);
    }

    private static void MostrarNotificacionLocal(int solicitudId, string titulo, string cuerpo)
    {
        try
        {
            // El ID de la notificación es el solicitudId para que sea único y reemplazable.
            // Si solicitudId llega en 0 (no debería), usamos uno aleatorio.
            int notifId = solicitudId > 0 ? solicitudId : new Random().Next(1000, 9999);

            var notif = new NotificationRequest
            {
                NotificationId = notifId,
                Title = titulo,
                Description = cuerpo,

                // ¡CRÍTICO! ReturningData es lo que llega en e.Request.ReturningData
                // cuando el usuario toca la notificación. Debe ser el solicitudId como string.
                ReturningData = solicitudId.ToString(),

                BadgeNumber = 1,
                CategoryType = NotificationCategoryType.Status,
                Android =
                {
                    ChannelId = "acceso_control",
                    Priority = AndroidPriority.High,
                    IsGroupSummary = false,
                    // AutoCancel: la notificación desaparece cuando el usuario la toca
                    AutoCancel = true,
                }
            };

            LocalNotificationCenter.Current.Show(notif);

            System.Diagnostics.Debug.WriteLine(
                $"[FCM] Notificación local mostrada. NotifId={notifId} ReturningData='{solicitudId}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FCM Error Local Notif]: {ex.Message}");
        }
    }
}