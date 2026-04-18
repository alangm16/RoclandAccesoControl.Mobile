using Android.App;
using Android.Content;          // <-- 1. Agregado para el Intent
using Android.Content.PM;
using Android.Gms.Tasks;
using Android.OS;
using Android.Util;
using Android.Widget;
using AndroidX.AppCompat.App;
using Firebase;
using Firebase.Messaging;
using Plugin.LocalNotification; // <-- 2. Agregado para conectar el plugin
using System.Threading.Tasks;

namespace RoclandAccesoControl.Mobile;

// 3. EL LAUNCHMODE ES CRÍTICO AQUÍ:
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    LaunchMode = LaunchMode.SingleTask, // <-- ESTO FALTABA
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
    ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize |
    ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var app = FirebaseApp.InitializeApp(this);
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;

        _ = GetFcmTokenAsync();
        CrearCanalNotificaciones();
        SolicitarPermisoNotificaciones();

        if (Intent != null)
        {
            // ALERTA VISUAL 1

            //Toast.MakeText(this, "APP ABIERTA DESDE NOTIFICACIÓN (OnCreate)", ToastLength.Long)?.Show();
            LocalNotificationCenter.NotifyNotificationTapped(Intent);
        }
    }


    // 4. ESTE ES EL PUENTE FALTANTE. Sin esto, el tap se pierde en el vacío.
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent != null)
        {
            // ALERTA VISUAL 2
            //Toast.MakeText(this, "VOLVIÓ AL FRENTE POR NOTIFICACIÓN (OnNewIntent)", ToastLength.Long)?.Show();
            LocalNotificationCenter.NotifyNotificationTapped(intent);
        }
    }

    private async System.Threading.Tasks.Task GetFcmTokenAsync()
    {
        try
        {
            var androidTask = FirebaseMessaging.Instance.GetToken();
            string token = await androidTask.ToSystemTask();
            Android.Util.Log.Debug("FCM", $"Token obtenido: {token}");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("FCM", $"Error al obtener token: {ex.Message}");
        }
    }

    private void CrearCanalNotificaciones()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var canal = new NotificationChannel(
            "acceso_control",
            "Control de Acceso",
            NotificationImportance.High)
        {
            Description = "Notificaciones de solicitudes de acceso"
        };
        canal.EnableVibration(true);
        canal.EnableLights(true);

        var manager = GetSystemService(NotificationService) as NotificationManager;
        manager?.CreateNotificationChannel(canal);
    }

    private void SolicitarPermisoNotificaciones()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+
        {
            RequestPermissions(
                new[] { Android.Manifest.Permission.PostNotifications }, 101);
        }
    }

    public override void OnRequestPermissionsResult(
        int requestCode, string[] permissions, Permission[] grantResults)
    {
        if (requestCode == 101
            && grantResults.Length > 0
            && grantResults[0] == Permission.Granted)
        {
            System.Diagnostics.Debug.WriteLine("[FCM] Permiso de notificaciones concedido.");
        }
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}