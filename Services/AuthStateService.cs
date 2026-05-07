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
            var request = new LoginRequest { Username = username, Password = password };
            var loginResponse = await _apiService.LoginSuperAdminAsync(username, password);

            return await ProcesarSesionExitosa(loginResponse.Token);
        }
        catch (Exception ex)
        {
            // Manejar error (credenciales inválidas, etc.)
            return false;
        }
    }

    public async Task<bool> IniciarSesionPorQrAsync(string qrCode)
    {
        try
        {
            // 1. Llamamos a Super Admin. Fíjate que ya no creamos el objeto QrLoginRequest, 
            // le pasamos el string directo porque así lo definimos en el ApiService.
            var loginResponse = await _apiService.LoginQrSuperAdminAsync(qrCode);

            // 2. Validamos que realmente nos haya devuelto un token
            if (string.IsNullOrEmpty(loginResponse?.Token))
                return false;

            // 3. Continuamos con el paso 2 (Obtener perfil y guardar)
            return await ProcesarSesionExitosa(loginResponse.Token);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> ProcesarSesionExitosa(string token)
    {
        // A. Inyectamos el JWT en el HttpClient para que la siguiente petición funcione
        _apiService.SetAuthToken(token);

        // B. Obtenemos el perfil local desde Acceso Control
        var perfil = await _apiService.ObtenerMiPerfilAsync();

        if (perfil is null)
            return false;

        // C. Usamos el método que ya tienes creado en AuthStateService para guardar todo
        GuardarSesion(token, perfil.NombreCompleto, perfil.Id);

        return true;
    }
}