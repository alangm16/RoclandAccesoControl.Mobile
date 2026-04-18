using RoclandAccesoControl.Mobile.ViewModels;

namespace RoclandAccesoControl.Mobile.Views;

public partial class DetalleSolicitudPage : ContentPage
{
    public DetalleSolicitudPage(DetalleSolicitudViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}