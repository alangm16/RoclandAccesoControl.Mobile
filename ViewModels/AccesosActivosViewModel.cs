using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Models;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.Views.Popups;
using System.Collections.ObjectModel;

namespace RoclandAccesoControl.Mobile.ViewModels;

public partial class AccesosActivosViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;
    private readonly SignalRService _signalR;

    [ObservableProperty] private ObservableCollection<AccesoActivo> _activos = [];
    [ObservableProperty] private int _totalDentro;
    [ObservableProperty] private bool _sinActivos = true;

    public AccesosActivosViewModel(ApiService api, AuthStateService auth, SignalRService signalR)
    {
        _api = api;
        _auth = auth;
        _signalR = signalR;
        Titulo = "Dentro ahora";

        _signalR.SalidaRegistrada += OnSalidaRegistrada;
    }

    private void OnSalidaRegistrada(int registroId)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Buscar siempre por ID para asegurar la referencia correcta
            var itemARemover = Activos.FirstOrDefault(a => a.RegistroId == registroId);
            if (itemARemover != null)
            {
                Activos.Remove(itemARemover);
                TotalDentro = Activos.Count;
                SinActivos = Activos.Count == 0;
            }
        });
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        EstaCargando = true;
        try
        {
            var lista = await _api.ObtenerActivosAsync();
            Activos = new ObservableCollection<AccesoActivo>(lista);
            TotalDentro = lista.Count;
            SinActivos = lista.Count == 0;
        }
        catch (Exception ex)
        {
            var toast = new ErrorToast("Sin conexión", ex.Message);
            await Shell.Current.CurrentPage.ShowPopupAsync(toast);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task MarcarSalidaAsync(AccesoActivo activo)
    {
        if (activo is null) return;

        var popup = new ConfirmarSalidaPopup(activo);
        var popupResult = await Shell.Current.CurrentPage.ShowPopupAsync<bool>(popup);
        if (!popupResult.Result) return;

        EstaCargando = true;
        try
        {
            var ok = await _api.MarcarSalidaAsync(new MarcarSalidaRequest
            {
                RegistroId = activo.RegistroId,
                TipoRegistro = activo.TipoRegistro
            });

            if (ok)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var itemARemover = Activos.FirstOrDefault(x => x.RegistroId == activo.RegistroId);
                    if (itemARemover != null)
                    {
                        Activos.Remove(itemARemover);
                        TotalDentro = Activos.Count;
                        SinActivos = Activos.Count == 0;
                    }
                });
            }
            else
            {
                var toast = new ErrorToast("Error al registrar", "No se pudo registrar la salida.");
                await Shell.Current.CurrentPage.ShowPopupAsync(toast);
            }
        }
        catch (Exception ex)
        {
            var toast = new ErrorToast("Error de red", ex.Message);
            await Shell.Current.CurrentPage.ShowPopupAsync(toast);
        }
        finally
        {
            EstaCargando = false;
        }
    }
}