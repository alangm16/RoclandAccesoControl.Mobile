using RoclandAccesoControl.Mobile.ViewModels;

namespace RoclandAccesoControl.Mobile.Views;

public partial class AccesosActivosPage : ContentPage
{
    private readonly AccesosActivosViewModel _vm;

    public AccesosActivosPage(AccesosActivosViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null);
    }
}