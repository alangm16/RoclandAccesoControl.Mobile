namespace RoclandAccesoControl.Mobile.Services;

public interface IDocumentScannerService
{
    /// <summary>
    /// Abre el escáner de documentos nativo de ML Kit.
    /// Retorna los bytes de la imagen capturada, o null si el usuario canceló.
    /// </summary>
    Task<byte[]?> EscanearDocumentoAsync();
}