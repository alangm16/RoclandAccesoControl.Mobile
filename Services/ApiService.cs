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

        var handler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
            }
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    // ── Auth — Credenciales ────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(string usuario, string password)
    {
        var body = JsonContent.Create(new { usuario, password });
        var resp = await _http.PostAsync("/api/mob/accesocontrol/Auth/guardia/login", body);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
    }

    // ── Auth — Código QR ───────────────────────────────────────────────
    /// <summary>
    /// Envía el contenido del QR escaneado al backend y recibe la misma
    /// <see cref="LoginResponse"/> que el login por credenciales.
    /// Ajusta la ruta si tu controller usa un path distinto.
    /// </summary>
    public async Task<LoginResponse?> LoginQrAsync(string codigoQR)
    {
        var body = JsonContent.Create(new { codigoQR });
        var resp = await _http.PostAsync("/api/mob/accesocontrol/Auth/guardia/login-qr", body);
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

    // ── Gafetes disponibles ────────────────────────────────────────────
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

    // ── Obtener solicitud por ID ───────────────────────────────────────
    public async Task<SolicitudPendiente?> ObtenerSolicitudPorIdAsync(int id)
    {
        SetAuthHeader();
        var resp = await _http.GetAsync($"/api/mob/accesocontrol/Guardias/solicitudes/{id}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<SolicitudPendiente>(JsonOpts);
    }

    // ── Helper ─────────────────────────────────────────────────────────
    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _auth.Token);
    }
}