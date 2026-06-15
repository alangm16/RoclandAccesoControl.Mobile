using CommunityToolkit.Maui.Views;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class FotoRequeridaPopup : Popup<bool>
{
    public FotoRequeridaPopup()
    {
        InitializeComponent();
    }

    private async void OnCancelar(object sender, TappedEventArgs e) => await CloseAsync(false);
    private async void OnTomarFoto(object sender, TappedEventArgs e) => await CloseAsync(true);
}