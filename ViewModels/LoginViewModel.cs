using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.Views;

namespace RoclandAccesoControl.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;
    private readonly FcmTokenService _fcmTokenService;

    [ObservableProperty] private string _usuario = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private bool _hayError;

    public LoginViewModel(ApiService api, AuthStateService auth, FcmTokenService fcmTokenService)
    {
        _api = api;
        _auth = auth;
        _fcmTokenService = fcmTokenService;
        Titulo = "Guardia — Acceso";
    }

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            MostrarError("Ingresa usuario y contraseña.");
            return;
        }

        EstaCargando = true;
        HayError = false;

        try
        {
            // PASO 1: Login Directo en Super Admin (requiere proyecto y plataforma)
            var loginResponse = await _api.LoginDirectoAsync(Usuario, Password);

            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                MostrarError("Usuario o contraseña incorrectos.");
                return;
            }

            // Inyectamos el JWT en el HttpClient
            _api.SetAuthToken(loginResponse.Token);

            // PASO 2: Obtener el perfil local de Acceso Control
            var perfil = await _api.ObtenerMiPerfilAsync();

            if (perfil is null)
            {
                MostrarError("No tienes un perfil asignado en Acceso Control.");
                return;
            }

            // Guardamos la sesión usando el PerfilId correcto
            _auth.GuardarSesion(loginResponse.Token, perfil.NombreCompleto, perfil.PerfilId);

            // Registrar token FCM (contra SuperAdmin)
            await _fcmTokenService.RegistrarTokenAsync();

            // Navegar al shell principal
            await Shell.Current.GoToAsync("//Bitacora");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Login Error] {ex.Message}");
            MostrarError("Error de conexión al iniciar sesión.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    public async Task IniciarSesionQrAsync(string qr)
    {
        if (string.IsNullOrWhiteSpace(qr))
        {
            MostrarError("Código QR vacío.");
            return;
        }

        EstaCargando = true;
        HayError = false;

        try
        {
            // PASO 1: Login QR en Super Admin
            var loginResponse = await _api.LoginQrAsync(qr);

            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                MostrarError("QR no reconocido o inválido.");
                return;
            }

            // Inyectamos el JWT
            _api.SetAuthToken(loginResponse.Token);

            // PASO 2: Obtener el perfil local
            var perfil = await _api.ObtenerMiPerfilAsync();

            if (perfil is null)
            {
                MostrarError("No tienes un perfil asignado en Acceso Control.");
                return;
            }

            // Guardamos la sesión
            _auth.GuardarSesion(loginResponse.Token, perfil.NombreCompleto, perfil.PerfilId);

            // Registrar token FCM
            await _fcmTokenService.RegistrarTokenAsync();

            await Shell.Current.GoToAsync("//Bitacora");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QR Login Error] {ex.Message}");
            MostrarError("Error de conexión al validar QR.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void MostrarError(string msg)
    {
        MensajeError = msg;
        HayError = true;
    }
}