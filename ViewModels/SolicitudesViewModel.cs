using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using RoclandAccesoControl.Mobile.Models;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.Views;
using System.Collections.ObjectModel;

namespace RoclandAccesoControl.Mobile.ViewModels;

public partial class SolicitudesViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;
    private readonly AuthStateService _auth;

    [ObservableProperty] private ObservableCollection<SolicitudPendiente> _solicitudes = [];
    [ObservableProperty] private string _estadoConexion = "Conectando...";
    [ObservableProperty] private Color _colorEstadoConexion = Colors.Orange;
    [ObservableProperty] private int _cantidadPendientes;
    [ObservableProperty] private bool _sinSolicitudes = true;

    public SolicitudesViewModel(ApiService api, SignalRService signalR, AuthStateService auth)
    {
        _api = api;
        _signalR = signalR;
        _auth = auth;
        Titulo = "Solicitudes";

        _signalR.NuevaSolicitudRecibida += OnNuevaSolicitud;
        _signalR.EstadoConexionCambiado += OnEstadoCambiado;
        _signalR.SolicitudResuelta += OnSolicitudResuelta;

        // Escuchar cuando la aplicación despierte
        WeakReferenceMessenger.Default.Register<AppResumedMessage>(this, async (r, m) =>
        {
            // Forzar recarga silenciosa de la lista para traer solicitudes que llegaron mientras la pantalla estaba apagada
            _ = CargarSolicitudesAsync();

            // Si la conexión de SignalR murió por el reposo del SO, la volvemos a iniciar
            if (_signalR.Estado != HubConnectionState.Connected)
            {
                await ConectarSignalRAsync();
            }
        });
    }

    [RelayCommand]
    public async Task InicializarAsync()
    {
        await CargarSolicitudesAsync();
        await ConectarSignalRAsync();
    }

    [RelayCommand]
    public async Task CargarSolicitudesAsync()
    {
        EstaCargando = true;
        try
        {
            var lista = await _api.ObtenerSolicitudesAsync();
            Solicitudes = new ObservableCollection<SolicitudPendiente>(lista);
            CantidadPendientes = lista.Count;
            SinSolicitudes = lista.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Error", $"No se pudieron cargar las solicitudes: {ex.Message}", "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task VerDetallAsync(SolicitudPendiente solicitud)
    {
        if (solicitud is null) return;
        await Shell.Current.GoToAsync(nameof(DetalleSolicitudPage),
            new Dictionary<string, object> { { "Solicitud", solicitud } });
    }

    [RelayCommand]
    private async Task IrAActivosAsync() =>
        await Shell.Current.GoToAsync("//AccesosActivos");

    [RelayCommand]
    private void CerrarSesion()
    {
        _auth.CerrarSesion();
        Shell.Current.GoToAsync("//Login");
    }

    private async Task ConectarSignalRAsync()
    {
        try
        {
            await _signalR.ConectarAsync();
        }
        catch (Exception ex)
        {
            EstadoConexion = "Sin conexión";
            ColorEstadoConexion = Colors.Red;
            await Shell.Current.DisplayAlert(
                "SignalR", $"No se pudo conectar: {ex.Message}", "OK");
        }
    }

    private async void OnNuevaSolicitud(NuevaSolicitudEvent evento)
    {
        var solicitud = new SolicitudPendiente
        {
            SolicitudId = evento.SolicitudId,
            RegistroId = evento.RegistroId,
            TipoRegistro = evento.TipoRegistro,
            NombrePersona = evento.NombrePersona,
            Empresa = evento.Empresa,
            NumeroIdentificacion = evento.NumeroIdentificacion,
            TipoID = evento.TipoID,
            Motivo = evento.Motivo,
            Area = evento.Area,
            FechaSolicitud = evento.FechaSolicitud
        };

        // Lanzar la notificación local (esto puede ir fuera del hilo principal)
        EnviarNotificacionLocal(solicitud);

        // OBLIGATORIO: Modificar la interfaz y la colección en el Hilo Principal
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Solicitudes.Insert(0, solicitud);
            CantidadPendientes = Solicitudes.Count;
            SinSolicitudes = Solicitudes.Count == 0;

            // Forzar una notificación de cambio de propiedad para que el binding se actualice
            OnPropertyChanged(nameof(Solicitudes));
        });
    }

    private void OnEstadoCambiado(HubConnectionState estado)
    {
        (EstadoConexion, ColorEstadoConexion) = estado switch
        {
            HubConnectionState.Connected => ("● Conectado", Color.FromArgb("#22C55E")),
            HubConnectionState.Reconnecting => ("◌ Reconectando...", Colors.Orange),
            _ => ("✕ Desconectado", Colors.Red)
        };

        // Si SignalR se acaba de reconectar de forma automática, recargar la lista
        if (estado == HubConnectionState.Connected)
        {
            // Llamada "Fire-and-forget" para no trabar el hilo
            _ = CargarSolicitudesAsync();
        }
    }

    private static void EnviarNotificacionLocal(SolicitudPendiente s)
    {
        var notification = new NotificationRequest
        {
            NotificationId = s.SolicitudId,
            Title = $"Nueva solicitud — {s.TipoRegistro}",
            Description = $"{s.NombrePersona} · {s.Motivo}",
            BadgeNumber = 1,
            CategoryType = NotificationCategoryType.Status,
            Android =
            {
                ChannelId = "acceso_control",
                Priority = AndroidPriority.High,
                IsGroupSummary = false
            }
        };
        LocalNotificationCenter.Current.Show(notification);
    }

    private void OnSolicitudResuelta(int solicitudId, string estado)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (solicitudId > 0)
            {
                // Tenemos el id exacto: quitar solo esa solicitud
                var item = Solicitudes.FirstOrDefault(s => s.SolicitudId == solicitudId);
                if (item is not null)
                {
                    Solicitudes.Remove(item);
                    CantidadPendientes = Solicitudes.Count;
                    SinSolicitudes = Solicitudes.Count == 0;
                }
            }
            else
            {
                // El servidor no mandó un id parseable: recargar toda la lista
                // para mantener sincronía (fallback seguro)
                await CargarSolicitudesAsync();
            }
        });
    }
}