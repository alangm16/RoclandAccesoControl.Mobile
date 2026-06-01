using RoclandAccesoControl.Mobile.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RoclandAccesoControl.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly IServiceProvider _serviceProvider;

    private static string BaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? AppConstants.BaseUrlAndroid
            : AppConstants.BaseUrlWindows;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

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

    // ─────────────────────────────────────────────────────────────────
    // GESTIÓN DE TOKENS (LAZY LOADING PARA EVITAR CIRCULAR DEPENDENCY)
    // ─────────────────────────────────────────────────────────────────

    private void SetAuthHeader()
    {
        // Obtenemos el AuthStateService bajo demanda, así MAUI no hace un bucle infinito
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !string.IsNullOrEmpty(authService.Token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);
        }
    }

    // Inyecta el token manualmente (usado por LoginViewModel durante el login de 2 pasos)
    public void SetAuthToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // ENDPOINTS DE SUPER ADMIN (Identidad Global)

    public async Task<LoginResponse?> LoginDirectoAsync(string username, string password)
    {
        try
        {
            var payload = new
            {
                Username = username,
                Password = password,
                CodigoProyecto = AppConstants.CodigoProyectoAccesoControl,
                Plataforma = AppConstants.PlataformaMobile
            };
            var response = await _http.PostAsJsonAsync("api/superadmin/Auth/login-directo", payload);
            if (!response.IsSuccessStatusCode) return null;
            var rawJson = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[LOGIN DIRECTO] {rawJson}");
            return JsonSerializer.Deserialize<LoginResponse>(rawJson, JsonOpts);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginDirecto] Error: {ex.Message}");
            return null;
        }
    }

    public async Task<LoginResponse?> LoginQrAsync(string qrCode)
    {
        try
        {
            var payload = new
            {
                QrCode = qrCode,
                CodigoProyecto = AppConstants.CodigoProyectoAccesoControl,
                Plataforma = AppConstants.PlataformaMobile
            };
            var response = await _http.PostAsJsonAsync("api/superadmin/Auth/login-qr", payload);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<LoginResponse?> RefrescarTokenAsync(string refreshToken)
    {
        try
        {
            var payload = new { RefreshToken = refreshToken };
            var response = await _http.PostAsJsonAsync("api/superadmin/Auth/refresh", payload);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }


    // ─────────────────────────────────────────────────────────────────
    // ENDPOINTS DE ACCESO CONTROL (Perfil Local)
    // ─────────────────────────────────────────────────────────────────

    public async Task<MiPerfilResponse?> ObtenerMiPerfilAsync()
    {
        try
        {
            // Ajusta esta ruta a tu nuevo endpoint de mi-perfil en el controlador local
            var response = await _http.GetAsync("api/mob/accesocontrol/Auth/mi-perfil");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<MiPerfilResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // OPERACIONES DE LA APP
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<SolicitudPendiente>> ObtenerSolicitudesAsync()
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !await authService.GarantizarTokenValidoAsync())
            return new List<SolicitudPendiente>();

        SetAuthHeader();
        var resp = await _http.GetAsync("/api/mob/accesocontrol/Guardias/solicitudes");
        if (!resp.IsSuccessStatusCode) return new();
        return await resp.Content.ReadFromJsonAsync<List<SolicitudPendiente>>(JsonOpts) ?? new();
    }

    public async Task<List<AccesoActivo>> ObtenerActivosAsync()
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !await authService.GarantizarTokenValidoAsync())
            return new List<AccesoActivo>();

        SetAuthHeader();
        var resp = await _http.GetAsync("/api/mob/accesocontrol/Guardias/activos");
        if (!resp.IsSuccessStatusCode) return new();
        return await resp.Content.ReadFromJsonAsync<List<AccesoActivo>>(JsonOpts) ?? new();
    }

    public async Task<bool> AprobarAsync(AprobarRequest request)
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !await authService.GarantizarTokenValidoAsync())
            return false;

        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync("/api/mob/accesocontrol/Guardias/aprobar", request);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RechazarAsync(RechazarRequest request)
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !await authService.GarantizarTokenValidoAsync())
            return false;

        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync("/api/mob/accesocontrol/Guardias/rechazar", request);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<GafeteDisponible>> ObtenerGafetesDisponiblesAsync()
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !await authService.GarantizarTokenValidoAsync())
            return new List<GafeteDisponible>();

        SetAuthHeader();
        var resp = await _http.GetAsync("/api/mob/accesocontrol/Guardias/gafetes/disponibles");
        if (!resp.IsSuccessStatusCode) return new();
        return await resp.Content.ReadFromJsonAsync<List<GafeteDisponible>>(JsonOpts) ?? new();
    }

    public async Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request)
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !await authService.GarantizarTokenValidoAsync())
            return false;

        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync("/api/mob/accesocontrol/Guardias/salida", request);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RegistrarFcmTokenSuperAdminAsync(string fcmToken, string? deviceToken = null, string? dispositivoInfo = null)
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !await authService.GarantizarTokenValidoAsync())
            return false;

        SetAuthHeader();
        var payload = new
        {
            FcmToken = fcmToken,
            DeviceToken = deviceToken,
            DispositivoInfo = dispositivoInfo ?? DeviceInfo.Name
        };
        try
        {
            var response = await _http.PostAsJsonAsync("api/superadmin/Dispositivos/token", payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<SolicitudPendiente?> ObtenerSolicitudPorIdAsync(int id)
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !await authService.GarantizarTokenValidoAsync())
            return null;

        SetAuthHeader();
        var resp = await _http.GetAsync($"/api/mob/accesocontrol/Guardias/solicitudes/{id}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<SolicitudPendiente>(JsonOpts);
    }
}