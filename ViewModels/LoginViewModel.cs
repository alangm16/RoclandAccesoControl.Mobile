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
            var result = await _api.LoginAsync(Usuario, Password);

            if (result is null)
            {
                MostrarError("Usuario o contraseña incorrectos.");
                return;
            }

            _auth.GuardarSesion(result.Token, result.Nombre, result.Id);

            await _fcmTokenService.RegistrarTokenAsync();

            // Navegar al shell principal
            await Shell.Current.GoToAsync("//Bitacora");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error de conexión";
            MostrarError(errorMsg);
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
            // Ajusta esta llamada a tu API real
            var result = await _api.LoginQrAsync(qr);

            if (result is null)
            {
                MostrarError("QR no reconocido.");
                return;
            }

            _auth.GuardarSesion(result.Token, result.Nombre, result.Id);
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
}