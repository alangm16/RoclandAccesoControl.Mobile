using CommunityToolkit.Maui.Views;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class ConfirmarVerFotoPopup : Popup<bool>
{
    public ConfirmarVerFotoPopup()
    {
        InitializeComponent();
    }

    private async void OnContinuarSinVer(object sender, TappedEventArgs e)
        => await CloseAsync(false);

    private async void OnVerFoto(object sender, TappedEventArgs e)
        => await CloseAsync(true);
}