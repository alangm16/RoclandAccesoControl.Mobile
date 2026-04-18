using CommunityToolkit.Maui.Views;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class ErrorToast : Popup
{
    public ErrorToast(string titulo, string mensaje)
    {
        InitializeComponent();
        LblTitulo.Text = titulo;
        LblMensaje.Text = mensaje;

        // Auto-cierre a los 1.5 segundos
        Task.Delay(1500).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(() => CloseAsync()));
    }
}