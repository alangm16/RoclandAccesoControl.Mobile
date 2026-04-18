using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using RoclandAccesoControl.Mobile.ViewModels;

namespace RoclandAccesoControl.Mobile.Views;

public partial class SolicitudesPage : ContentPage
{
    private readonly SolicitudesViewModel _vm;

    public SolicitudesPage(SolicitudesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarCommand.ExecuteAsync(null);
    }

    private void TestLocalNotification()
    {
        var notif = new NotificationRequest
        {
            NotificationId = 999,
            Title = "Prueba",
            Description = "¿Funciona?",
            Android = { ChannelId = "acceso_control" }
        };
        LocalNotificationCenter.Current.Show(notif);
    }
}