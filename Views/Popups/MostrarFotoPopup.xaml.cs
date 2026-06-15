using CommunityToolkit.Maui.Views;
using System.Windows.Input;

namespace RoclandAccesoControl.Mobile.Views.Popups;

public partial class MostrarFotoPopup : Popup
{
    public ICommand CerrarCommand { get; }

    public MostrarFotoPopup(ImageSource imagen)
    {
        InitializeComponent();
        FotoImage.Source = imagen;
        CerrarCommand = new Command(async () => await CloseAsync());
        BindingContext = this;
    }
}