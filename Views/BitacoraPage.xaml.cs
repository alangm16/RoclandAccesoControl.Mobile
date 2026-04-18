using RoclandAccesoControl.Mobile.ViewModels;
using System.ComponentModel;

namespace RoclandAccesoControl.Mobile.Views;

public partial class BitacoraPage : ContentPage
{
    private readonly BitacoraViewModel _vm;
    private double _pageWidth;

    public BitacoraPage(BitacoraViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.PropertyChanged += VmOnPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarAsync();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0) return;

        _pageWidth = width;

        // El indicador ocupa la mitad del ancho
        TabIndicator.WidthRequest = width / 2;

        // Mover indicador a la posición del tab activo
        TabIndicator.TranslationX = _vm.TabActivo * (width / 2);
    }

    private async void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BitacoraViewModel.TabActivo))
            return;

        if (_pageWidth <= 0)
            return;

        double targetX = _vm.TabActivo * (_pageWidth / 2);

        await TabIndicator.TranslateTo(
            targetX,
            0,
            180,              // duración (ms)
            Easing.CubicOut   // easing suave
        );
    }
}