using System.Collections.Concurrent;
using Amqp0_9_1.Utilities.Extensions;

namespace Amqp0_9_1.Utilities.Async
{
    internal class AsyncConcurrentQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new();
        private volatile TaskCompletionSource<bool>? _tcs;

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
            var tcs = _tcs;
            if (tcs?.TrySetResult(true) == true)
                Interlocked.CompareExchange(ref _tcs, null, tcs);
        }

        public async Task<T> DequeueAsync(CancellationToken ct = default)
        {
            if (_queue.TryDequeue(out var result))
                return result;

            while (true)
            {
                if (_queue.TryDequeue(out result))
                    return result;

                var tcs = GetOrCreateTcs();
                var completed = await Task.WhenAny(tcs.Task, ct.AsTask()).ConfigureAwait(false);
                if (completed == ct.AsTask())
                    throw new OperationCanceledException(ct);
            }
        }

        private TaskCompletionSource<bool> GetOrCreateTcs()
        {
            var newTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var current = Interlocked.CompareExchange(ref _tcs, newTcs, null);
            return current ?? newTcs;
        }
    }
}
