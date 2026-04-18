using CommunityToolkit.Mvvm.ComponentModel;

namespace RoclandAccesoControl.Mobile.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoEstaCargando))]
    private bool _estaCargando;

    [ObservableProperty]
    private string _titulo = string.Empty;

    public bool NoEstaCargando => !EstaCargando;
}