namespace Amqp0_9_1.Utilities.Extensions
{
    internal static class CancellationTokenExtensions
    {
        public static Task AsTask(this CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            ct.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs);
            return tcs.Task;
        }
    }
}
