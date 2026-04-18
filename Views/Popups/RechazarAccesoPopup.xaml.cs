using CommunityToolkit.Maui.Views;

namespace RoclandAccesoControl.Mobile.Views.Popups;

// Retorna null si canceló, string (vacío o con texto) si confirmó
public partial class RechazarAccesoPopup : Popup<string?>
{
    public RechazarAccesoPopup(string nombrePersona)
    {
        InitializeComponent();
        LblNombre.Text = nombrePersona;
    }

    private async void OnCancelar(object sender, TappedEventArgs e) => await CloseAsync(null);
    private async void OnConfirmar(object sender, TappedEventArgs e) => await CloseAsync(EntryMotivo.Text ?? "");
}