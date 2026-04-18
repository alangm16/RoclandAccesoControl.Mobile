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
}