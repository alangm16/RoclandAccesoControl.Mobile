using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RoclandAccesoControl.Mobile.Models;

namespace RoclandAccesoControl.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly AuthStateService _auth;

    // URL base — se lee desde appsettings o constante de compilación
    private static string BaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? AppConstants.BaseUrlAndroid
            : AppConstants.BaseUrlWindows;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(AuthStateService auth)
    {
        _auth = auth;

        // Utilizamos SocketsHttpHandler para tener control total sobre el ciclo de vida de los sockets TCP
        var handler = new SocketsHttpHandler
        {
            // 1. Si la conexión lleva 30 segundos sin usarse, se cierra automáticamente.
            // Esto evita usar un socket "zombie" que se quedó colgado al suspender la VM.
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),

            // 2. Fuerza a crear una conexión totalmente nueva cada 2 minutos como máximo, 
            // renovando el estado de la red.
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),

            // 3. Envía pings internos para verificar si el servidor sigue vivo a nivel TCP
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(10),

            // 4. Ignora errores de certificado SSL (equivalente a lo que ya tenías para desarrollo local)
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
            }
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),

            // El timeout por defecto de HttpClient es de 100 segundos.
            // Lo reducimos a 15 segundos para que, si la VM está apagada, 
            // la app no se quede "congelada" cargando por minuto y medio antes de dar el error.
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    // ── Auth ───────────────────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(string usuario, string password)
    {
        var body = JsonContent.Create(new { usuario, password });
        var resp = await _http.PostAsync("/api/mob/accesocontrol/Auth/guardia/login", body);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
    }

    // ── Solicitudes ────────────────────────────────────────────────────
    public async Task<List<SolicitudPendiente>> ObtenerSolicitudesAsync()
    {
        SetAuthHeader();
        var resp = await _http.GetAsync("/api/mob/accesocontrol/Guardias/solicitudes");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<SolicitudPendiente>>(JsonOpts) ?? [];
    }

    // ── Accesos activos ────────────────────────────────────────────────
    public async Task<List<AccesoActivo>> ObtenerActivosAsync()
    {
        SetAuthHeader();
        var resp = await _http.GetAsync("/api/mob/accesocontrol/Guardias/activos");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<AccesoActivo>>(JsonOpts) ?? [];
    }

    // ── Aprobar ────────────────────────────────────────────────────────
    public async Task<bool> AprobarAsync(AprobarRequest request)
    {
        SetAuthHeader();
        var resp = await _http.PostAsync("/api/mob/accesocontrol/Guardias/aprobar",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    // ── Rechazar ───────────────────────────────────────────────────────
    public async Task<bool> RechazarAsync(RechazarRequest request)
    {
        SetAuthHeader();
        var resp = await _http.PostAsync("/api/mob/accesocontrol/Guardias/rechazar",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<GafeteDisponible>> ObtenerGafetesDisponiblesAsync()
    {
        SetAuthHeader();
        var resp = await _http.GetAsync("/api/mob/accesocontrol/Guardias/gafetes/disponibles");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<GafeteDisponible>>(JsonOpts) ?? [];
    }

    // ── Marcar salida ──────────────────────────────────────────────────
    public async Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request)
    {
        SetAuthHeader();
        var resp = await _http.PostAsync("/api/mob/accesocontrol/Guardias/salida",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _auth.Token);
    }

    // ── Registrar token FCM ────────────────────────────────────────────
    public async Task<bool> RegistrarFcmTokenAsync(int guardiaId, string fcmToken)
    {
        SetAuthHeader();
        var resp = await _http.PostAsync("/api/mob/accesocontrol/Guardias/fcm-token",
            new StringContent(
                JsonSerializer.Serialize(new { guardiaId, fcmToken }),
                Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    // ── Obtener Solicitud por ID (Para Deep Linking / Notificaciones) ──
    public async Task<SolicitudPendiente?> ObtenerSolicitudPorIdAsync(int id)
    {
        SetAuthHeader();

        // NOTA: Ajusta la ruta "/api/guardias/solicitud/{id}" si tu endpoint 
        // en el backend (Controller) tiene un nombre diferente.
        var resp = await _http.GetAsync($"/api/mob/accesocontrol/Guardias/solicitudes/{id}");

        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadFromJsonAsync<SolicitudPendiente>(JsonOpts);
    }
}