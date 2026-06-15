using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Models;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.Views;
using RoclandAccesoControl.Mobile.Views.Popups;
using System.Collections.ObjectModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace RoclandAccesoControl.Mobile.ViewModels;

[QueryProperty(nameof(Solicitud), "Solicitud")]
[QueryProperty(nameof(SolicitudIdParam), "id")]
public partial class DetalleSolicitudViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;
    private readonly IMediaPicker _mediaPicker;

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

    public DetalleSolicitudViewModel(ApiService api, AuthStateService auth, IMediaPicker mediaPicker)
    {
        _api = api;
        _auth = auth;
        _mediaPicker = mediaPicker;
        Titulo = "Detalle de Solicitud";
    }

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
            OnPropertyChanged(nameof(NoHayGafetes));
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
        var popupResult = await Shell.Current.CurrentPage.ShowPopupAsync<GafeteDisponible?>(popup);
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

    // ─────────────────────────────────────────────────────────────────
    // FLUJO DE APROBACIÓN CON VERIFICACIÓN DE FOTO
    // ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AprobarAsync()
    {
        if (Solicitud is null) return;

        // Si la persona NO tiene foto, pedir captura antes de continuar
        if (!Solicitud.TieneFoto)
        {
            var tomarFoto = await Shell.Current.DisplayAlert(
                "Foto requerida",
                "Esta persona no tiene foto de identificación. ¿Desea capturarla ahora?",
                "Sí, tomar foto",
                "Cancelar");

            if (tomarFoto)
            {
                bool fotoSubida = await TomarFotoYSubirAsync();
                if (!fotoSubida)
                {
                    await Shell.Current.DisplayAlert("Cancelado", "No se pudo registrar la foto. La aprobación fue cancelada.", "OK");
                    return;
                }
                // Actualizar la propiedad local para que no vuelva a pedir en esta sesión
                Solicitud.TieneFoto = true;
            }
            else
            {
                return; // El guardia canceló
            }
        }

        // A partir de aquí, la persona tiene foto (ya sea porque ya la tenía o se acaba de subir)
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

    /// <summary>
    /// Captura una foto usando la cámara y la sube al backend.
    /// </summary>
    private async Task<bool> TomarFotoYSubirAsync()
    {
        try
        {
            if (Solicitud!.PersonaId == 0)
            {
                var recargada = await _api.ObtenerSolicitudPorIdAsync(Solicitud.SolicitudId);
                if (recargada == null || recargada.PersonaId == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "No se pudo identificar a la persona.", "OK");
                    return false;
                }
                Solicitud = recargada;
            }

            var photo = await _mediaPicker.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Tomar foto de identificación"
            });

            if (photo == null) return false;

            using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();

            EstaCargando = true;
            bool success = await _api.SubirFotoPersonaAsync(Solicitud!.PersonaId, imageBytes, photo.ContentType);
            if (success)
            {
                await Shell.Current.DisplayAlert("Éxito", "Foto guardada correctamente", "OK");
                return true;
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo guardar la foto en el servidor", "OK");
                return false;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al capturar la foto: {ex.Message}", "OK");
            return false;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // RESTO DE MÉTODOS (VerificarDatosAsync, MostrarCancelacionVerificacion,
    // RechazarAsync, RegresarAsync, NavegarAtrasOSolicitudesAsync)
    // se mantienen igual, solo agregamos el using de Microsoft.Maui.Media
    // ─────────────────────────────────────────────────────────────────

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

    private async Task NavegarAtrasOSolicitudesAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await Task.Delay(100);
                var navigationStack = Shell.Current.Navigation.NavigationStack;
                bool hayPaginaAnteriorValida = navigationStack.Count >= 2 &&
                                                navigationStack[^2] is not DetalleSolicitudPage;

                if (hayPaginaAnteriorValida)
                    await Shell.Current.GoToAsync("..");
                else
                    await Shell.Current.GoToAsync("//Bitacora");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NAV Error] {ex.Message}");
                await Shell.Current.GoToAsync("//Bitacora");
            }
        });
    }

    [RelayCommand]
    private async Task VerFotoAsync()
    {
        if (Solicitud == null) return;

        if (Solicitud.PersonaId == 0)
        {
            var recargada = await _api.ObtenerSolicitudPorIdAsync(Solicitud.SolicitudId);
            if (recargada?.PersonaId > 0) Solicitud = recargada;
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", "No se pudo identificar a la persona.", "OK");
                return;
            }
        }

        try
        {
            var stream = await _api.ObtenerFotoPersonaAsync(Solicitud.PersonaId);
            if (stream == null)
            {
                await Shell.Current.DisplayAlertAsync("Sin foto", "No hay foto registrada para esta persona.", "OK");
                return;
            }
            var imageSource = ImageSource.FromStream(() => stream);
            var popup = new MostrarFotoPopup(imageSource);
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"No se pudo cargar la foto: {ex.Message}", "OK");
        }
    }
}