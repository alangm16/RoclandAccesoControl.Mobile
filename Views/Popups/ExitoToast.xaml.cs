using CommunityToolkit.Maui.Views;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class ExitoToast : Popup
{
    public ExitoToast(string titulo, string mensaje)
    {
        InitializeComponent();
        LblTitulo.Text = titulo;
        LblMensaje.Text = mensaje;

        Task.Delay(1500).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(() => CloseAsync()));
    }
}