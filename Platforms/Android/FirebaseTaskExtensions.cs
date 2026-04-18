using System.Threading.Tasks;
using Android.Gms.Tasks;

public static class FirebaseTaskExtensions
{
    public static Task<string> ToSystemTask(this Android.Gms.Tasks.Task task)
    {
        var tcs = new TaskCompletionSource<string>();

        task.AddOnSuccessListener(new OnSuccessListener(result => tcs.SetResult(result?.ToString())))
            .AddOnFailureListener(new OnFailureListener(ex => tcs.SetException(ex)));

        return tcs.Task;
    }

    class OnSuccessListener : Java.Lang.Object, IOnSuccessListener
    {
        private readonly Action<Java.Lang.Object> _onSuccess;
        public OnSuccessListener(Action<Java.Lang.Object> onSuccess) => _onSuccess = onSuccess;
        public void OnSuccess(Java.Lang.Object result) => _onSuccess(result);
    }

    class OnFailureListener : Java.Lang.Object, IOnFailureListener
    {
        private readonly Action<Java.Lang.Exception> _onFailure;
        public OnFailureListener(Action<Java.Lang.Exception> onFailure) => _onFailure = onFailure;
        public void OnFailure(Java.Lang.Exception e) => _onFailure(e);
    }
}