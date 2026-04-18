using Microsoft.AspNetCore.SignalR.Client;
using RoclandAccesoControl.Mobile.Models;
using System.Text.Json;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace RoclandAccesoControl.Mobile.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly AuthStateService _auth;

    public event Action<NuevaSolicitudEvent>? NuevaSolicitudRecibida;
    public event Action<int, string>? SolicitudResuelta; // (solicitudId, estado)
    public event Action<HubConnectionState>? EstadoConexionCambiado;
    public event Action<int>? SalidaRegistrada;

    public HubConnectionState Estado =>
        _connection?.State ?? HubConnectionState.Disconnected;

    public SignalRService(AuthStateService auth)
    {
        _auth = auth;
    }

    public async Task ConectarAsync()
    {
        if (_connection?.State == HubConnectionState.Connected) return;

        var hubUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? AppConstants.BaseUrlAndroid + AppConstants.SignalRHubPath
            : AppConstants.BaseUrlWindows + AppConstants.SignalRHubPath;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(_auth.Token);

                options.HttpMessageHandlerFactory = _ =>
                {
#if ANDROID
                    return new Xamarin.Android.Net.AndroidMessageHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
#else
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
#endif
                };
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        _connection.On<int>("SalidaRegistrada", registroId =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                SalidaRegistrada?.Invoke(registroId));
        });

        // ── NuevaSolicitud ─────────────────────────────────────────────
        _connection.On<NuevaSolicitudEvent>("NuevaSolicitud", solicitud =>
        {
            // 1. Construir y lanzar la notificación local a través de SignalR
            try
            {
                var notif = new NotificationRequest
                {
                    NotificationId = solicitud.SolicitudId,
                    Title = $"Nueva Solicitud - {solicitud.TipoRegistro}",
                    Description = $"{solicitud.NombrePersona} - {solicitud.Motivo}",
                    ReturningData = solicitud.SolicitudId.ToString(), // <-- EL ID PARA EL DEEP LINKING
                    BadgeNumber = 1,
                    CategoryType = NotificationCategoryType.Status,
                    Android =
                    {
                        ChannelId = "acceso_control",
                        Priority = AndroidPriority.High,
                        IsGroupSummary = false
                    }
                };
                LocalNotificationCenter.Current.Show(notif);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR Local Notif Error]: {ex.Message}");
            }

            // 2. Notificamos a la interfaz de usuario (por si el guardia ya está viendo la lista)
            MainThread.BeginInvokeOnMainThread(() =>
                NuevaSolicitudRecibida?.Invoke(solicitud));
        });

        // ── SolicitudResuelta ──────────────────────────────────────────
        // El servidor puede enviar un objeto JSON con SolicitudId y Estado.
        // Deserializamos manualmente para extraer el id real y notificar la UI.
        _connection.On<JsonElement>("SolicitudResuelta", data =>
        {
            int id = 0;
            string estado = "Resuelta";

            try
            {
                // El servidor puede enviar { solicitudId: 5, estado: "Aprobada" }
                // Intentamos con variantes de capitalización
                if (data.ValueKind == JsonValueKind.Object)
                {
                    if (data.TryGetProperty("solicitudId", out var idProp)
                        || data.TryGetProperty("SolicitudId", out idProp))
                        id = idProp.GetInt32();

                    if (data.TryGetProperty("estado", out var estadoProp)
                        || data.TryGetProperty("Estado", out estadoProp))
                        estado = estadoProp.GetString() ?? estado;
                }
                else if (data.ValueKind == JsonValueKind.Number)
                {
                    // El servidor envía el id directamente como número
                    id = data.GetInt32();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SignalR] Error parseando SolicitudResuelta: {ex.Message}");
            }

            MainThread.BeginInvokeOnMainThread(() =>
                SolicitudResuelta?.Invoke(id, estado));
        });



        // ── Estado de conexión ─────────────────────────────────────────
        _connection.Reconnecting += error =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                EstadoConexionCambiado?.Invoke(HubConnectionState.Reconnecting));
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                EstadoConexionCambiado?.Invoke(HubConnectionState.Connected));
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                EstadoConexionCambiado?.Invoke(HubConnectionState.Disconnected));
            return Task.CompletedTask;
        };

        System.Diagnostics.Debug.WriteLine($"[SIGNALR TOKEN DEBUG]: '{_auth.Token}'");

        await _connection.StartAsync();
        EstadoConexionCambiado?.Invoke(HubConnectionState.Connected);
    }

    public async Task DesconectarAsync()
    {
        if (_connection is not null)
            await _connection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}