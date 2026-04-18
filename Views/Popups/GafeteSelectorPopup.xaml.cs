using CommunityToolkit.Maui.Views;
using RoclandAccesoControl.Mobile.Models;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class GafeteSelectorPopup : Popup<GafeteDisponible?>
{
    public GafeteSelectorPopup(IEnumerable<GafeteDisponible> gafetes)
    {
        InitializeComponent();

        var lista = gafetes.ToList();
        ListaGafetes.ItemsSource = lista;
        LblConteo.Text = $"{lista.Count} disponibles";
    }

    private async void OnGafeteTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is GafeteDisponible gafete)
        {
            await CloseAsync(gafete);
        }
    }
}