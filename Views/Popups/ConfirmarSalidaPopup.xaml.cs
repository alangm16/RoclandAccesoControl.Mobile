using CommunityToolkit.Maui.Views;
using RoclandAccesoControl.Mobile.Models;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class ConfirmarSalidaPopup : Popup<bool>
{
    public ConfirmarSalidaPopup(AccesoActivo activo)
    {
        InitializeComponent();
        LblNombre.Text = activo.NombrePersona;
        LblGafete.Text = $"Gafete #{activo.NumeroGafete}";
    }

    private async void OnCancelar(object sender, TappedEventArgs e)
    {
        await CloseAsync(false);
    }

    private async void OnConfirmar(object sender, TappedEventArgs e)
    {
        await CloseAsync(true);
    }
}