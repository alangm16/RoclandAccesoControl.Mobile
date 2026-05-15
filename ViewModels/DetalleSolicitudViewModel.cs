using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Models;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.Views;
using RoclandAccesoControl.Mobile.Views.Popups;
using System.Collections.ObjectModel;

namespace RoclandAccesoControl.Mobile.ViewModels;

[QueryProperty(nameof(Solicitud), "Solicitud")]
[QueryProperty(nameof(SolicitudIdParam), "id")]
public partial class DetalleSolicitudViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;
    public bool NoHayGafetes => GafetesDisponibles.Count == 0;

    [ObservableProperty] private SolicitudPendiente? _solicitud;
    [ObservableProperty] private ObservableCollection<GafeteDisponible> _gafetesDisponibles = [];
    [ObservableProperty] private GafeteDisponible? _gafeteSeleccionado;
    [ObservableProperty] private bool _accionCompletada;

    public string SolicitudIdParam
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                _ = CargarSolicitudDesdeApiAsync(id);
            }
        }
    }

    public DetalleSolicitudViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        Titulo = "Detalle de Solicitud";
    }

    // Cargar gafetes cuando se asigna la solicitud o al navegar con id
    partial void OnSolicitudChanged(SolicitudPendiente? value)
    {
        if (value != null)
            _ = CargarGafetesAsync();
    }

    private async Task CargarGafetesAsync()
    {
        try
        {
            var lista = await _api.ObtenerGafetesDisponiblesAsync();
            GafetesDisponibles = new ObservableCollection<GafeteDisponible>(lista);
            OnPropertyChanged(nameof(NoHayGafetes)); // <-- Añadir esto
        }
        catch (Exception ex)
        {
            var toast = new ErrorToast("Sin gafetes", "No se pudieron cargar los gafetes disponibles.");
            await Shell.Current.CurrentPage.ShowPopupAsync(toast);
        }
    }

    [RelayCommand]
    private async Task AbrirSelectorGafeteAsync()
    {
        if (GafetesDisponibles.Count == 0)
            return;

        var popup = new GafeteSelectorPopup(GafetesDisponibles);

        var popupResult =
            await Shell.Current.CurrentPage
                .ShowPopupAsync<GafeteDisponible?>(popup);

        var seleccionado = popupResult.Result;

        if (seleccionado is not null)
            GafeteSeleccionado = seleccionado;
    }

    private async Task CargarSolicitudDesdeApiAsync(int id)
    {
        EstaCargando = true;
        try
        {
            Solicitud = await _api.ObtenerSolicitudPorIdAsync(id);
        }
        catch (Exception ex)
        {
            var toast = new ErrorToast("Error al cargar", "No se pudo cargar el detalle del visitante.");
            await Shell.Current.CurrentPage.ShowPopupAsync(toast);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task AprobarAsync()
    {
        if (Solicitud is null) return;

        bool datosVerificados = await VerificarDatosAsync();
        if (!datosVerificados) return;

        if (GafeteSeleccionado is null)
        {
            var toast = new ErrorToast("Gafete requerido", "Debes seleccionar un gafete disponible.");
            await Shell.Current.CurrentPage.ShowPopupAsync(toast);
            return;
        }

        var popup = new ConfirmarAprobacionPopup(Solicitud.NombrePersona, GafeteSeleccionado.Codigo);
        var popupResult = await Shell.Current.CurrentPage.ShowPopupAsync<bool>(popup);
        if (!popupResult.Result) return;

        EstaCargando = true;
        try
        {
            var ok = await _api.AprobarAsync(new AprobarRequest
            {
                SolicitudId = Solicitud.SolicitudId,
                GafeteId = GafeteSeleccionado.Id
            });

            if (ok)
            {
                var toast = new ExitoToast("Acceso aprobado", $"Entrega el gafete {GafeteSeleccionado.Codigo}.");
                await Shell.Current.CurrentPage.ShowPopupAsync(toast);
                await NavegarAtrasOSolicitudesAsync();
            }
            else
            {
                var toast = new ErrorToast("No se pudo aprobar", "Intenta de nuevo o recarga los gafetes.");
                await Shell.Current.CurrentPage.ShowPopupAsync(toast);
                await CargarGafetesAsync();
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

    private async Task<bool> VerificarDatosAsync()
    {
        // Verificar nombre
        var nombrePopup = new VerificarDatoPopup(
            "Verificar nombre",
            "¿El nombre en la INE coincide con el siguiente?",
            Solicitud!.NombrePersona);
        var nombreOk = await Shell.Current.CurrentPage.ShowPopupAsync<bool>(nombrePopup);
        if (!nombreOk.Result)
        {
            await MostrarCancelacionVerificacion();
            return false;
        }

        // Verificar identificación (tipo + número)
        string idCompleto = $"{Solicitud.TipoID}: {Solicitud.NumeroIdentificacion}";
        var idPopup = new VerificarDatoPopup(
            "Verificar identificación",
            "¿El tipo y número de identificación coinciden con la INE?",
            idCompleto);
        var idOk = await Shell.Current.CurrentPage.ShowPopupAsync<bool>(idPopup);
        if (!idOk.Result)
        {
            await MostrarCancelacionVerificacion();
            return false;
        }

        // Verificar placas si es proveedor/cliente y tiene placas registradas
        bool requierePlacas = (Solicitud.TipoRegistro == "Proveedor" || Solicitud.TipoRegistro == "Cliente")
                              && !string.IsNullOrWhiteSpace(Solicitud.Placas);
        if (requierePlacas)
        {
            var placasPopup = new VerificarDatoPopup(
                "Verificar placas",
                "¿Las placas del vehículo coinciden con las registradas?",
                Solicitud.Placas);
            var placasOk = await Shell.Current.CurrentPage.ShowPopupAsync<bool>(placasPopup);
            if (!placasOk.Result)
            {
                await MostrarCancelacionVerificacion();
                return false;
            }
        }

        return true;
    }

    private async Task MostrarCancelacionVerificacion()
    {
        var toast = new ErrorToast("Verificación cancelada", "Los datos no coinciden. No se puede aprobar.");
        await Shell.Current.CurrentPage.ShowPopupAsync(toast);
    }


    [RelayCommand]
    private async Task RechazarAsync()
    {
        if (Solicitud is null) return;

        var popup = new RechazarAccesoPopup(Solicitud.NombrePersona);
        var popupResult = await Shell.Current.CurrentPage.ShowPopupAsync<string?>(popup);
        string? motivo = popupResult.Result;
        if (motivo is null) return;

        EstaCargando = true;
        try
        {
            var ok = await _api.RechazarAsync(new RechazarRequest
            {
                SolicitudId = Solicitud.SolicitudId,
                Motivo = motivo
            });

            if (ok)
            {
                var toast = new ExitoToast("Acceso rechazado", "El registro fue guardado correctamente.");
                await Shell.Current.CurrentPage.ShowPopupAsync(toast);
                await NavegarAtrasOSolicitudesAsync();
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

    [RelayCommand]
    private async Task RegresarAsync() => await NavegarAtrasOSolicitudesAsync();

    // ──── Navegación inteligente hacia atrás ──────────────────────────
    private async Task NavegarAtrasOSolicitudesAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                // Pequeño retardo para asegurar que la UI termine cualquier animación
                await Task.Delay(100);

                var navigationStack = Shell.Current.Navigation.NavigationStack;
                bool hayPaginaAnteriorValida = navigationStack.Count >= 2 &&
                                                navigationStack[^2] is not DetalleSolicitudPage;

                if (hayPaginaAnteriorValida)
                {
                    // Flujo normal: regresar a la página anterior (normalmente SolicitudesPage)
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Fallback: navegar directamente a la raíz de Solicitudes
                    await Shell.Current.GoToAsync("//Bitacora");
                }
            }
            catch (Exception ex)
            {
                // Si algo falla, último recurso: ir a la raíz de Solicitudes
                System.Diagnostics.Debug.WriteLine($"[NAV Error] {ex.Message}");
                await Shell.Current.GoToAsync("//Bitacora");
            }
        });
    }
}