using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Services;

namespace RoclandAccesoControl.Mobile.ViewModels;

public partial class BitacoraViewModel : BaseViewModel
{
    // ─── ViewModels hijos (inyectados) ────────────────────────────
    public SolicitudesViewModel SolicitudesVm { get; }
    public AccesosActivosViewModel ActivosVm { get; }

    private readonly AuthStateService _auth;

    // ─── Estado del tab ───────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrandoSolicitudes))]
    [NotifyPropertyChangedFor(nameof(MostrandoActivos))]
    [NotifyPropertyChangedFor(nameof(TituloActivo))]
    [NotifyPropertyChangedFor(nameof(SubtituloActivo))]
    [NotifyPropertyChangedFor(nameof(ColorSubtitulo))]
    [NotifyPropertyChangedFor(nameof(TienePendientes))]
    private int _tabActivo;   // 0 = Solicitudes, 1 = Activos

    // ─── Propiedades derivadas ─────────────────────────────────────

    public bool MostrandoSolicitudes => TabActivo == 0;
    public bool MostrandoActivos     => TabActivo == 1;

    public bool TienePendientes =>
        SolicitudesVm.CantidadPendientes > 0;

    public string TituloActivo => TabActivo == 0 ? "Solicitudes" : "Dentro ahora";

    public string SubtituloActivo => TabActivo == 0
        ? SolicitudesVm.EstadoConexion
        : "Personas en instalaciones";

    public Color ColorSubtitulo => TabActivo == 0
        ? SolicitudesVm.ColorEstadoConexion
        : Color.FromArgb("#6B7280");

    // ─────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────

    public BitacoraViewModel(
        SolicitudesViewModel solicitudesVm,
        AccesosActivosViewModel activosVm,
        AuthStateService auth)
    {
        SolicitudesVm = solicitudesVm;
        ActivosVm     = activosVm;
        _auth         = auth;

        // Redibujar propiedades derivadas cuando cambia el estado de conexión
        SolicitudesVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SolicitudesViewModel.EstadoConexion)
                               or nameof(SolicitudesViewModel.ColorEstadoConexion)
                               or nameof(SolicitudesViewModel.CantidadPendientes))
            {
                OnPropertyChanged(nameof(SubtituloActivo));
                OnPropertyChanged(nameof(ColorSubtitulo));
                OnPropertyChanged(nameof(TienePendientes));
            }
        };
    }

    // ─────────────────────────────────────────────────────────────
    // Inicialización (llamado desde OnAppearing)
    // ─────────────────────────────────────────────────────────────

    public async Task InicializarAsync()
    {
        // Inicializa ambas vistas en paralelo para mayor velocidad
        await Task.WhenAll(
            SolicitudesVm.InicializarCommand.ExecuteAsync(null),
            ActivosVm.CargarCommand.ExecuteAsync(null)
        );
    }

    // ─────────────────────────────────────────────────────────────
    // Comandos
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado al tocar un tab de la barra superior.
    /// </summary>
    [RelayCommand]
    private void SeleccionarTab(string parametro)
    {
        if (int.TryParse(parametro, out int tab))
            TabActivo = tab;
    }

    /// <summary>
    /// Llama al comando de refresco del tab activo.
    /// </summary>
    [RelayCommand]
    private async Task RefrescarAsync()
    {
        if (TabActivo == 0)
            await SolicitudesVm.CargarSolicitudesCommand.ExecuteAsync(null);
        else
            await ActivosVm.CargarCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Cierra sesión.
    /// </summary>
    [RelayCommand]
    private void CerrarSesion()
    {
        _auth.CerrarSesion();
        Shell.Current.GoToAsync("//Login");
    }

    // ─────────────────────────────────────────────────────────────
    // Método interno para actualizar el tab DESDE el code-behind
    // (cuando el usuario desliza) sin provocar un segundo scroll.
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Actualiza TabActivo sin disparar la animación de scroll
    /// (ya que el scroll fue iniciado por el usuario).
    /// </summary>
    public void SetTabSilently(int tab)
    {
        if (_tabActivo == tab) return;

        // Usamos SetProperty para notificar a la UI sin pasar por el comando
        SetProperty(ref _tabActivo, tab, nameof(TabActivo));

        // Notificar propiedades derivadas manualmente
        OnPropertyChanged(nameof(MostrandoSolicitudes));
        OnPropertyChanged(nameof(MostrandoActivos));
        OnPropertyChanged(nameof(TituloActivo));
        OnPropertyChanged(nameof(SubtituloActivo));
        OnPropertyChanged(nameof(ColorSubtitulo));
        OnPropertyChanged(nameof(TienePendientes));
    }
}
