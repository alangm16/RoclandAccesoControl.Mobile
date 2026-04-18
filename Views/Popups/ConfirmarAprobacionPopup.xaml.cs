using CommunityToolkit.Maui.Views;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class ConfirmarAprobacionPopup : Popup<bool>
{
    public ConfirmarAprobacionPopup(string nombrePersona, string codigoGafete)
    {
        InitializeComponent();
        LblNombre.Text = nombrePersona;
        LblGafete.Text = $"Gafete asignado: {codigoGafete}";
    }

    private async void OnCancelar(object sender, TappedEventArgs e) => await CloseAsync(false);
    private async void OnConfirmar(object sender, TappedEventArgs e) => await CloseAsync(true);
}