using System.Collections.Concurrent;

namespace Amqp0_9_1.Utilities.Async
{
    internal class AsyncConcurrentDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dict = new();
        private readonly ConcurrentDictionary<TKey, TaskCompletionSource<TValue>> _waiters = new();

        public async Task<TValue> GetMessageByKeyAsync(TKey key, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_dict.TryGetValue(key, out var existing))
                    return existing;

                var tcs = _waiters.GetOrAdd(
                    key,
                    _ => new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously));

                if (_dict.TryGetValue(key, out var afterAdd))
                {
                    _waiters.TryRemove(key, out _);
                    tcs.TrySetResult(afterAdd);
                    return afterAdd;
                }

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCts.Token.Register(() =>
                    tcs.TrySetCanceled(linkedCts.Token));


                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _dict.TryRemove(key, out _);
            }
        }

        public bool AddOrUpdate(TKey key, TValue value)
        {
            var added = _dict.AddOrUpdate(key,
                _ => value,
                (_, __) => value);

            if (_waiters.TryRemove(key, out var tcs))
            {
                tcs.TrySetResult(value);
            }

            return added != null;
        }
    }
}