using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.ViewModels;
using RoclandAccesoControl.Mobile.Views;
using ZXing.Net.Maui.Controls;
namespace RoclandAccesoControl.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification()   // Plugin.LocalNotification — solo inicialización, sin iOS
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        // ── Servicios ──────────────────────────────────────────────────
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SignalRService>();
        builder.Services.AddSingleton<AuthStateService>();
        builder.Services.AddSingleton<FcmTokenService>();

        // ── ViewModels ─────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<SolicitudesViewModel>();
        builder.Services.AddTransient<DetalleSolicitudViewModel>();
        builder.Services.AddTransient<AccesosActivosViewModel>();
        builder.Services.AddSingleton<BitacoraViewModel>();

        // ── Pages ──────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<SolicitudesPage>();
        builder.Services.AddTransient<DetalleSolicitudPage>();
        builder.Services.AddTransient<AccesosActivosPage>();
        builder.Services.AddSingleton<BitacoraPage>();

        builder.Services.AddSingleton<IMediaPicker>(MediaPicker.Default);

#if ANDROID
        builder.Services.AddSingleton<IDocumentScannerService,
            RoclandAccesoControl.Mobile.Platforms.Android.DocumentScannerService>();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}