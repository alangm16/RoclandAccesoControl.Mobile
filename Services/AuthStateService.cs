using RoclandAccesoControl.Mobile.Models;

namespace RoclandAccesoControl.Mobile.Services;

/// <summary>
/// Mantiene el estado de autenticación durante la sesión de la app.
/// </summary>
public class AuthStateService
{
    public string Token { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public string NombreGuardia { get; private set; } = string.Empty;
    public int GuardiaId { get; private set; }
    public DateTime TokenExpiracion { get; private set; }
    public bool EstaAutenticado => !string.IsNullOrEmpty(Token);

    private readonly ApiService _apiService;

    public AuthStateService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void GuardarSesion(string token, string refreshToken, string nombre, int id, DateTime expiracionToken)
    {
        Token = token;
        RefreshToken = refreshToken;
        NombreGuardia = nombre;
        GuardiaId = id;
        TokenExpiracion = expiracionToken;

        SecureStorage.Default.SetAsync("jwt_token", token);
        SecureStorage.Default.SetAsync("refresh_token", refreshToken);
        SecureStorage.Default.SetAsync("guardia_nombre", nombre);
        SecureStorage.Default.SetAsync("guardia_id", id.ToString());
        SecureStorage.Default.SetAsync("token_expira", expiracionToken.ToString("O"));

        // Límite absoluto de sesión: 8 horas desde ahora
        var expiracionAbsoluta = DateTime.Now.AddHours(8);
        SecureStorage.Default.SetAsync("sesion_absoluta_expira", expiracionAbsoluta.ToString("O"));

        // Actualizar header del HttpClient
        _apiService.SetAuthToken(token);
    }

    public async Task<bool> RestaurarSesionAsync()
    {
        try
        {
            if (!await EsSesionAbsolutaValidaAsync())
            {
                CerrarSesion();
                return false;
            }

            var token = await SecureStorage.Default.GetAsync("jwt_token");
            var refreshToken = await SecureStorage.Default.GetAsync("refresh_token");
            var nombre = await SecureStorage.Default.GetAsync("guardia_nombre");
            var idStr = await SecureStorage.Default.GetAsync("guardia_id");
            var expiraStr = await SecureStorage.Default.GetAsync("token_expira");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(idStr))
                return false;

            Token = token;
            RefreshToken = refreshToken ?? "";
            NombreGuardia = nombre ?? "";
            GuardiaId = int.Parse(idStr);
            if (DateTime.TryParse(expiraStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expira))
                TokenExpiracion = expira;

            _apiService.SetAuthToken(token);

            // Validar/refrescar token si es necesario
            if (!await GarantizarTokenValidoAsync())
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> GarantizarTokenValidoAsync()
    {
        if (!EstaAutenticado) return false;

        // 1. Verificar límite absoluto de 8 horas
        if (!await EsSesionAbsolutaValidaAsync())
        {
            CerrarSesion();
            return false;
        }

        // 2. Si el token aún es válido (margen 2 minutos), no hacer nada
        if (DateTime.UtcNow < TokenExpiracion.AddMinutes(-2))
            return true;

        // 3. Token expirado → intentar refresh
        if (string.IsNullOrEmpty(RefreshToken)) return false;

        var nuevoLogin = await _apiService.RefrescarTokenAsync(RefreshToken);
        if (nuevoLogin == null || string.IsNullOrEmpty(nuevoLogin.Token))
        {
            CerrarSesion();
            return false;
        }

        // 4. Actualizar credenciales
        GuardarSesion(nuevoLogin.Token, nuevoLogin.RefreshToken, NombreGuardia, GuardiaId, nuevoLogin.Expiracion);
        Token = nuevoLogin.Token;
        RefreshToken = nuevoLogin.RefreshToken;
        TokenExpiracion = nuevoLogin.Expiracion;
        _apiService.SetAuthToken(nuevoLogin.Token);

        return true;
    }

    private async Task<bool> EsSesionAbsolutaValidaAsync()
    {
        var expiraStr = await SecureStorage.Default.GetAsync("sesion_absoluta_expira");
        if (string.IsNullOrEmpty(expiraStr)) return false;

        if (DateTime.TryParse(expiraStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiraAbsoluta))
            return DateTime.Now < expiraAbsoluta;

        return false;
    }

    public void CerrarSesion()
    {
        Token = string.Empty;
        RefreshToken = string.Empty;
        NombreGuardia = string.Empty;
        GuardiaId = 0;
        TokenExpiracion = DateTime.MinValue;

        SecureStorage.Default.Remove("jwt_token");
        SecureStorage.Default.Remove("refresh_token");
        SecureStorage.Default.Remove("guardia_nombre");
        SecureStorage.Default.Remove("guardia_id");
        SecureStorage.Default.Remove("token_expira");
        SecureStorage.Default.Remove("sesion_absoluta_expira");
    }

    public async Task<bool> IniciarSesionAsync(string username, string password)
    {
        try
        {
            var loginResponse = await _apiService.LoginDirectoAsync(username, password);
            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
                return false;
            return await ProcesarSesionExitosa(loginResponse);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IniciarSesionPorQrAsync(string qrCode)
    {
        try
        {
            var loginResponse = await _apiService.LoginQrAsync(qrCode);
            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
                return false;
            return await ProcesarSesionExitosa(loginResponse);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProcesarSesionExitosa(LoginResponse loginResponse)
    {
        _apiService.SetAuthToken(loginResponse.Token);

        var perfil = await _apiService.ObtenerMiPerfilAsync();
        if (perfil is null)
            return false;

        GuardarSesion(loginResponse.Token, loginResponse.RefreshToken, perfil.NombreCompleto, perfil.PerfilId, loginResponse.Expiracion);
        return true;
    }
}