using Android.App;
using Android.Content;
using Google.Android.Gms.Extensions;
using Net.Google.MlKit.DocumentScanner;
using RoclandAccesoControl.Mobile.Services;

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
            .SetGalleryImportAllowed(false)   // Solo cámara, sin galería
            .SetPageLimit(1)                  // Solo 1 página (la credencial)
            .SetResultFormats(GmsDocumentScannerOptions.ResultFormatJpeg)
            .Build();

        var scanner = GmsDocumentScanning.GetClient(options);

        try
        {
            // Obtener el IntentSender para lanzar la UI de ML Kit
            var senderTask = await scanner.GetStartScanIntentAsync(activity);

            // Lanzar la actividad y esperar resultado
            DocumentScannerResultCallback.Register(_tcs);
            activity.StartIntentSenderForResult(
                senderTask,
                DocumentScannerResultCallback.RequestCode,
                null, 0, 0, 0);
        }
        catch (Exception ex)
        {
            _tcs.TrySetException(ex);
        }

        return await _tcs.Task;
    }
}