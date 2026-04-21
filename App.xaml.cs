using RoclandAccesoControl.Mobile.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace RoclandAccesoControl.Mobile;

public partial class App : Application
{
    private readonly AuthStateService _auth;

    // ID de solicitud que llega por tap ANTES de que OnStart() termine.
    private string? _idNotificacionPendiente = null;

    // Se vuelve true cuando OnStart() ya navegó a //Bitacora y el Shell está listo.
    private bool _sesionLista = false;

    public App(AuthStateService auth)
    {
        InitializeComponent();
        _auth = auth;

        MainPage = new AppShell();

        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
    }

    private void OnNotificationTapped(NotificationActionEventArgs e)
    {
        string data = e.Request?.ReturningData ?? string.Empty;
        System.Diagnostics.Debug.WriteLine($"[NOTIF TAP] ReturningData = '{data}' | SesionLista = {_sesionLista}");

        //MainThread.BeginInvokeOnMainThread(async () =>
        //{
        //    await App.Current.MainPage.DisplayAlertAsync(
        //        "Debug Notificación",
        //        $"Data recibida: '{data}'\nSesión Lista: {_sesionLista}",
        //        "OK");
        //});

        if (string.IsNullOrEmpty(data))
            return;

        if (_sesionLista)
        {
            // App estaba abierta o en segundo plano con sesión activa → navegar directo.
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync($"DetalleSolicitudPage?id={data}");
                }
                catch (Exception ex)
                {
                    //await App.Current.MainPage.DisplayAlert("Error de Navegación", ex.Message, "OK");
                    System.Diagnostics.Debug.WriteLine($"[NOTIF TAP] Error navegación: {ex.Message}");
                }
            });
        }
        else
        {
            // App arrancó en frío → guardar el ID; OnStart() lo consumirá
            // después de restaurar la sesión y navegar a //Bitacora.
            _idNotificacionPendiente = data;
            System.Diagnostics.Debug.WriteLine($"[NOTIF TAP] ID guardado como pendiente: {data}");
        }
    }

    protected override async void OnStart()
    {
        base.OnStart();

        bool sesionRestaurada = false;
        try
        {
            sesionRestaurada = await _auth.RestaurarSesionAsync();
            await Shell.Current.GoToAsync(sesionRestaurada ? "//Bitacora" : "//Login");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            return;
        }

        // A partir de aquí el Shell ya está en su ruta raíz → marcamos la sesión como lista.
        _sesionLista = true;

        if (sesionRestaurada && !string.IsNullOrEmpty(_idNotificacionPendiente))
        {
            var id = _idNotificacionPendiente;
            _idNotificacionPendiente = null;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Pequeño delay para que //Bitacora termine de renderizar antes de hacer push.
                await Task.Delay(400);
                System.Diagnostics.Debug.WriteLine($"[NOTIF PENDIENTE] Navegando a DetalleSolicitudPage?id={id}");
                try
                {
                    await Shell.Current.GoToAsync($"DetalleSolicitudPage?id={id}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NOTIF PENDIENTE] Error navegación: {ex.Message}");
                }
            });
        }
        else
        {
            _idNotificacionPendiente = null;
        }
    }
}