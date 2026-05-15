using RoclandAccesoControl.Mobile.Models;

namespace RoclandAccesoControl.Mobile.Services;

/// <summary>
/// Mantiene el estado de autenticación durante la sesión de la app.
/// </summary>
public class AuthStateService
{
    public string Token { get; private set; } = string.Empty;
    public string NombreGuardia { get; private set; } = string.Empty;
    public int GuardiaId { get; private set; }
    public bool EstaAutenticado => !string.IsNullOrEmpty(Token);

    private readonly ApiService _apiService;

    public AuthStateService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void GuardarSesion(string token, string nombre, int id)
    {
        Token = token;
        NombreGuardia = nombre;
        GuardiaId = id;
        // Persistir en SecureStorage para sobrevivir reinicios
        SecureStorage.Default.SetAsync("jwt_token", token);
        SecureStorage.Default.SetAsync("guardia_nombre", nombre);
        SecureStorage.Default.SetAsync("guardia_id", id.ToString());
    }

    public async Task<bool> RestaurarSesionAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            var nombre = await SecureStorage.Default.GetAsync("guardia_nombre");
            var idStr = await SecureStorage.Default.GetAsync("guardia_id");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(idStr))
                return false;

            Token = token;
            NombreGuardia = nombre ?? "";
            GuardiaId = int.Parse(idStr);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void CerrarSesion()
    {
        Token = string.Empty;
        NombreGuardia = string.Empty;
        GuardiaId = 0;
        SecureStorage.Default.Remove("jwt_token");
        SecureStorage.Default.Remove("guardia_nombre");
        SecureStorage.Default.Remove("guardia_id");
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
        // Establecer token en el HttpClient
        _apiService.SetAuthToken(loginResponse.Token);

        // Obtener el perfil local desde Acceso Control
        var perfil = await _apiService.ObtenerMiPerfilAsync();
        if (perfil is null)
            return false;

        // Guardar sesión (el ID ahora es perfil.PerfilId)
        GuardarSesion(loginResponse.Token, perfil.NombreCompleto, perfil.PerfilId);
        return true;
    }
}