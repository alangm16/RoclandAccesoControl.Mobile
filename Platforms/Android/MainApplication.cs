using Android.App;
using Android.Runtime;

namespace RoclandAccesoControl.Mobile;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnCreate()
    {
        base.OnCreate();

        // Las notificaciones push cuando la app está cerrada las maneja
        // MyFirebaseMessagingService automáticamente.
        // La navegación al tap se gestiona en AppShell a través del Intent.
    }
}