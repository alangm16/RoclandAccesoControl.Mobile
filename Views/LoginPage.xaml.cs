using RoclandAccesoControl.Mobile.ViewModels;
using ZXing.Net.Maui;

namespace RoclandAccesoControl.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _vm;
    private bool _qrProcesando = false;
    private bool _flashOn = false;

    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Arranca siempre en el tab de credenciales
        ActivarTabCredenciales();
    }

    // ─────────────────────────────────────────────────────────────────
    // TABS
    // ─────────────────────────────────────────────────────────────────

    private void OnTabCredencialesClicked(object? sender, EventArgs? e)
        => ActivarTabCredenciales();

    private async void OnTabQRClicked(object? sender, EventArgs? e)
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await ShowToastAsync("Se necesita permiso de cámara");
            return;
        }

        ActivarTabQR();
    }

    /// <summary>Botón "Abrir cámara" del placeholder dentro de la tarjeta.</summary>
    private async void OnAbrirCamaraClicked(object? sender, EventArgs? e)
        => await OnTabQRClickedAsync();

    private async Task OnTabQRClickedAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await ShowToastAsync("Se necesita permiso de cámara");
            return;
        }

        ActivarTabQR();
    }

    private void OnVolverClicked(object? sender, EventArgs? e)
        => ActivarTabCredenciales();

    // ─────────────────────────────────────────────────────────────────
    // ACTIVAR TABS
    // ─────────────────────────────────────────────────────────────────

    private void ActivarTabCredenciales()
    {
        // Apagar escáner y flash si estaban activos
        if (_flashOn) _ = ApagarFlashAsync();
        if (QrScanner != null) QrScanner.IsDetecting = false;
        _qrProcesando = false;

        // Visibilidad
        ScrollLogin.IsVisible = true;
        PanelQR.IsVisible = false;
        PanelCredenciales.IsVisible = true;
        PanelQRPlaceholder.IsVisible = false;

        // Estilo tab activo → Credenciales
        TabCredencialesIndicator.BackgroundColor = Color.FromArgb("#4CAF50");
        BtnTabCredenciales.TextColor = Color.FromArgb("#1B3A1B");
        BtnTabCredenciales.FontAttributes = FontAttributes.Bold;

        // Estilo tab inactivo → QR
        TabQRIndicator.BackgroundColor = Colors.Transparent;
        BtnTabQR.TextColor = Color.FromArgb("#5A7A5A");
        BtnTabQR.FontAttributes = FontAttributes.None;

        ActualizarIconoFlash(false);
    }

    private void ActivarTabQR()
    {
        // Visibilidad
        ScrollLogin.IsVisible = true;   // mantenemos el scroll visible para ver los tabs
        PanelQR.IsVisible = true;
        PanelCredenciales.IsVisible = false;
        PanelQRPlaceholder.IsVisible = false;

        // Estilo tab activo → QR
        TabQRIndicator.BackgroundColor = Color.FromArgb("#4CAF50");
        BtnTabQR.TextColor = Color.FromArgb("#1B3A1B");
        BtnTabQR.FontAttributes = FontAttributes.Bold;

        // Estilo tab inactivo → Credenciales
        TabCredencialesIndicator.BackgroundColor = Colors.Transparent;
        BtnTabCredenciales.TextColor = Color.FromArgb("#5A7A5A");
        BtnTabCredenciales.FontAttributes = FontAttributes.None;

        // Reiniciar estado del escáner
        QrStatusLabel.Text = string.Empty;
        QrStatusLabel.IsVisible = false;
        QrErrorLabel.Text = string.Empty;
        QrErrorLabel.IsVisible = false;
        _qrProcesando = false;

        ReiniciarCamara();
    }

    // ─────────────────────────────────────────────────────────────────
    // CÁMARA QR
    // ─────────────────────────────────────────────────────────────────

    private void ReiniciarCamara()
    {
        if (PanelQR == null) return;

        bool flashEstaba = _flashOn;

        // Remover instancia anterior
        if (QrScanner != null)
        {
            PanelQR.Children.Remove(QrScanner);
            QrScanner = null;
        }

        // Nueva instancia
        QrScanner = new ZXing.Net.Maui.Controls.CameraBarcodeReaderView
        {
            IsDetecting = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        QrScanner.BarcodesDetected += OnQrDetected;
        PanelQR.Children.Insert(0, QrScanner);

        if (flashEstaba)
            QrScanner.IsTorchOn = true;

        _qrProcesando = false;
    }

    private async void OnRefrescarClicked(object? sender, EventArgs? e)
    {
        QrStatusLabel.Text = "Reiniciando cámara...";
        QrStatusLabel.IsVisible = true;

        ReiniciarCamara();

        QrStatusLabel.Text = "Cámara lista";
        await Task.Delay(800);
        QrStatusLabel.IsVisible = false;
    }

    private async void OnQrDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_qrProcesando) return;
        _qrProcesando = true;
        QrScanner.IsDetecting = false;

        string codigoQR = e.Results?.FirstOrDefault()?.Value ?? "";

        if (string.IsNullOrWhiteSpace(codigoQR))
        {
            _qrProcesando = false;
            QrScanner.IsDetecting = true;
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            QrStatusLabel.Text = "Verificando...";
            QrStatusLabel.IsVisible = true;

            try
            {
                // Delegar la lógica de login QR al ViewModel
                await _vm.IniciarSesionQrAsync(codigoQR);

                // Si el ViewModel no navegó (error), reiniciar escáner
                QrStatusLabel.IsVisible = false;
                await Task.Delay(400);
                _qrProcesando = false;
                QrScanner.IsDetecting = true;
            }
            catch (Exception)
            {
                QrStatusLabel.IsVisible = false;
                QrErrorLabel.Text = "Error de conexión";
                QrErrorLabel.IsVisible = true;
                await Task.Delay(1500);
                QrErrorLabel.IsVisible = false;
                _qrProcesando = false;
                QrScanner.IsDetecting = true;
            }
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // FLASH / LINTERNA
    // ─────────────────────────────────────────────────────────────────

    private void OnFlashClicked(object? sender, EventArgs? e)
    {
        _flashOn = !_flashOn;
        if (QrScanner != null)
            QrScanner.IsTorchOn = _flashOn;
        ActualizarIconoFlash(_flashOn);
    }

    private void ActualizarIconoFlash(bool encendido)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (BtnFlash == null) return;
            BtnFlash.BackgroundColor = encendido
                ? Color.FromArgb("#4CAF50")
                : Color.FromArgb("#33FFFFFF");
            BtnFlash.ImageSource = encendido ? "flash_on.png" : "flash_off.png";
        });
    }

    private async Task ApagarFlashAsync()
    {
        try
        {
            _flashOn = false;
            if (QrScanner != null) QrScanner.IsTorchOn = false;
            ActualizarIconoFlash(false);
        }
        catch { }
        await Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────
    // TOAST
    // ─────────────────────────────────────────────────────────────────

    private async Task ShowToastAsync(string message, int duration = 2000)
    {
        ToastLabel.Text = message;
        ToastFrame.IsVisible = true;
        ToastFrame.Opacity = 0;

        await ToastFrame.FadeToAsync(1, 250);
        await Task.Delay(duration);
        await ToastFrame.FadeToAsync(0, 250);
        ToastFrame.IsVisible = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // BACK BUTTON
    // ─────────────────────────────────────────────────────────────────

    protected override bool OnBackButtonPressed()
    {
        // Si el escáner QR está abierto, volver a credenciales en lugar de salir
        if (PanelQR?.IsVisible == true)
        {
            ActivarTabCredenciales();
            return true;
        }

        return base.OnBackButtonPressed();
    }
}