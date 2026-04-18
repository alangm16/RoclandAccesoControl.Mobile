using CommunityToolkit.Maui.Views;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class VerificarDatoPopup : Popup<bool>
{
    public VerificarDatoPopup(string titulo, string instruccion, string valorEsperado)
    {
        InitializeComponent();
        LblTitulo.Text = titulo;
        LblInstruccion.Text = instruccion;
        LblValorEsperado.Text = valorEsperado;
    }

    private async void OnSiCoincide(object sender, TappedEventArgs e) => await CloseAsync(true);
    private async void OnNoCoincide(object sender, TappedEventArgs e) => await CloseAsync(false);
}