using Android.App;
using Android.Content;
using Net.Google.MLKit.Vision.DocumentScanner;

namespace RoclandAccesoControl.Mobile.Platforms.Android;

/// <summary>
/// Captura el resultado de la actividad de ML Kit Document Scanner.
/// Se engancha al ciclo de vida de MainActivity.
/// </summary>
public static class DocumentScannerResultCallback
{
    public const int RequestCode = 0xD0C5; // Código arbitrario único

    private static TaskCompletionSource<byte[]?>? _tcs;

    public static void Register(TaskCompletionSource<byte[]?> tcs)
    {
        _tcs = tcs;
    }

    /// <summary>
    /// Llamar desde MainActivity.OnActivityResult
    /// </summary>
    public static bool HandleResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode != RequestCode || _tcs == null)
            return false;

        if (resultCode != Result.Ok || data == null)
        {
            _tcs.TrySetResult(null); // Canceló
            _tcs = null;
            return true;
        }

        try
        {
            var scanResult = GmsDocumentScanningResult.FromActivityResultIntent(data);
            var pages = scanResult?.Pages;

            if (pages == null || pages.Count == 0)
            {
                _tcs.TrySetResult(null);
                _tcs = null;
                return true;
            }

            // Leer la imagen como bytes desde el Uri
            var uri = pages[0].ImageUri;
            var context = global::Android.App.Application.Context;
            using var stream = context.ContentResolver!.OpenInputStream(uri!)!;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            _tcs.TrySetResult(ms.ToArray());
        }
        catch (Exception ex)
        {
            _tcs.TrySetException(ex);
        }
        finally
        {
            _tcs = null;
        }

        return true;
    }
}