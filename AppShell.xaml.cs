using RoclandAccesoControl.Mobile.Views;

namespace RoclandAccesoControl.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Ruta modal para el detalle de una solicitud (igual que antes)
        Routing.RegisterRoute(nameof(DetalleSolicitudPage), typeof(DetalleSolicitudPage));
    }
}
