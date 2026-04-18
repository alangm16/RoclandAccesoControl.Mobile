#if ANDROID
using Firebase.Messaging;
using RoclandAccesoControl.Mobile.Platforms.Android;
#endif

namespace RoclandAccesoControl.Mobile.Services;

/// <summary>
/// Lee el token FCM guardado por MyFirebaseMessagingService y lo registra en el servidor.
/// Llamar después de hacer login.
/// </summary>
public class FcmTokenService
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    public FcmTokenService(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
    }

    public async Task RegistrarTokenAsync()
    {
#if ANDROID
        try
        {
            // Siempre pedimos el token directamente a Firebase para evitar usar uno expirado en caché
            var tcs = new TaskCompletionSource<string>();

            FirebaseMessaging.Instance.GetToken()
                .AddOnCompleteListener(new OnCompleteListenerToken(tcs));

            var token = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(token))
            {
                // Lo guardamos localmente por si lo ocupamos en otro lado
                Preferences.Set(MyFirebaseMessagingService.PrefKey, token);

                // Lo enviamos a la BD
                await _api.RegistrarFcmTokenAsync(_auth.GuardiaId, token);
                System.Diagnostics.Debug.WriteLine($"[FCM] Token registrado con éxito: {token[..10]}...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[FCM] Firebase no devolvió un token.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FCM] Error registrando token: {ex.Message}");
        }
#else
        System.Diagnostics.Debug.WriteLine("[FCM] Plataforma no soportada.");
        await Task.CompletedTask;
#endif
    }
}

#if ANDROID
/// <summary>
/// Adaptador para obtener el token FCM de forma asíncrona desde la Task API de Java.
/// </summary>
internal class OnCompleteListenerToken : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
{
    private readonly TaskCompletionSource<string> _tcs;

    public OnCompleteListenerToken(TaskCompletionSource<string> tcs) => _tcs = tcs;

    public void OnComplete(Android.Gms.Tasks.Task task)
    {
        if (task.IsSuccessful && task.Result is Java.Lang.String token)
            _tcs.TrySetResult(token.ToString());
        else
            _tcs.TrySetResult(string.Empty);
    }
}
#endif