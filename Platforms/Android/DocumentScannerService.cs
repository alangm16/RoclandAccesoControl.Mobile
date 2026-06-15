using Android.App;
using Android.Content;
using Android.Gms.Extensions; // Espacio de nombres correcto para .AsAsync()
using Net.Google.MLKit.DocumentScanner;
using Net.Google.MLKit.Vision.DocumentScanner;
using RoclandAccesoControl.Mobile.Services;
using System;
using System.Threading.Tasks;

namespace RoclandAccesoControl.Mobile.Platforms.Android;

public class DocumentScannerService : IDocumentScannerService
{
    private TaskCompletionSource<byte[]?>? _tcs;

    public async Task<byte[]?> EscanearDocumentoAsync()
    {
        _tcs = new TaskCompletionSource<byte[]?>();

        var activity = (Activity)Platform.CurrentActivity!;

        // Configurar el escáner: modo FULL = UI completa con autocaptura
        var options = new GmsDocumentScannerOptions.Builder()
            .SetScannerMode(GmsDocumentScannerOptions.ScannerModeFull)
            .SetGalleryImportAllowed(false)   // Solo cámara para asegurar la captura real del visitante
            .SetPageLimit(1)                  // Solo 1 página (frente de la credencial)
            .SetResultFormats(GmsDocumentScannerOptions.ResultFormatJpeg)
            .Build();

        var scanner = GmsDocumentScanning.GetClient(options);

        try
        {
            // Convertir la Gms.Task de Android a una Task de C# e inferir el IntentSender
            var intentSender = await scanner.GetStartScanIntent(activity).AsAsync<IntentSender>();

            if (intentSender != null)
            {
                // Lanzar la actividad y registrar el puente de espera
                DocumentScannerResultCallback.Register(_tcs);

                activity.StartIntentSenderForResult(
                    intentSender,
                    DocumentScannerResultCallback.RequestCode,
                    null, 0, 0, 0);
            }
            else
            {
                _tcs.TrySetResult(null);
            }
        }
        catch (Exception ex)
        {
            _tcs.TrySetException(ex);
        }

        return await _tcs.Task;
    }
}