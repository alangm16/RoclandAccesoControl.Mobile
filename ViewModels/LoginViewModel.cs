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
        Titulo = "Guardia — Acceso";
        _fcmTokenService = fcmTokenService;
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
            // PASO 1: Login Global en Super Admin
            var tokenResult = await _api.LoginSuperAdminAsync(Usuario, Password);

            if (string.IsNullOrEmpty(tokenResult?.Token))
            {
                MostrarError("Usuario o contraseña incorrectos.");
                return;
            }

            // Inyectamos el JWT en el HttpClient para las siguientes peticiones
            _api.SetAuthToken(tokenResult.Token);

            // PASO 2: Obtener el perfil local de Acceso Control
            var perfil = await _api.ObtenerMiPerfilAsync();

            if (perfil is null)
            {
                MostrarError("No tienes un perfil asignado en Acceso Control.");
                return;
            }

            // Guardamos la sesión combinando el Token de SuperAdmin y los datos locales
            _auth.GuardarSesion(tokenResult.Token, perfil.NombreCompleto, perfil.Id);

            await _fcmTokenService.RegistrarTokenAsync();

            // Navegar al shell principal
            await Shell.Current.GoToAsync("//Bitacora");
        }
        catch (Exception ex)
        {
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
            // PASO 1: Login Global por QR en Super Admin
            var tokenResult = await _api.LoginQrSuperAdminAsync(qr);

            if (string.IsNullOrEmpty(tokenResult?.Token))
            {
                MostrarError("QR no reconocido o inválido.");
                return;
            }

            // Inyectamos el JWT
            _api.SetAuthToken(tokenResult.Token);

            // PASO 2: Obtener el perfil local
            var perfil = await _api.ObtenerMiPerfilAsync();

            if (perfil is null)
            {
                MostrarError("No tienes un perfil asignado en Acceso Control.");
                return;
            }

            // Guardamos la sesión
            _auth.GuardarSesion(tokenResult.Token, perfil.NombreCompleto, perfil.Id);

            await _fcmTokenService.RegistrarTokenAsync();

            await Shell.Current.GoToAsync("//Bitacora");
        }
        catch (Exception ex)
        {
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