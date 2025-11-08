using System.Collections.Concurrent;
using System.Threading.Channels;
using Amqp0_9_1.Primitives.Frames;
using Amqp0_9_1.Methods;

namespace Amqp0_9_1.Processors;

internal sealed class IncomeMethodProcessor
{
    private readonly ChannelReader<AmqpRawFrame> _methodChanelReader;
    private readonly ConcurrentDictionary<Type, Channel<AmqpMethod>> _waiters = new();

    public IncomeMethodProcessor(ChannelReader<AmqpRawFrame> methodChanelReader)
    {
        _methodChanelReader = methodChanelReader;
    }

    public async void ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var methodRawFrame = await _methodChanelReader.ReadAsync(cancellationToken);
            var amqpMethod = MethodFactory.Create(methodRawFrame);
            var channel = _waiters.GetOrAdd(amqpMethod.GetType(), Channel.CreateBounded<AmqpMethod>(1));
            await channel.Writer.WriteAsync(amqpMethod, cancellationToken);
        }
    }

    public async Task<T> ReadAsync<T>(CancellationToken cancellationToken)
        where T : AmqpMethod
    {
        var channel = _waiters.GetOrAdd(typeof(T), Channel.CreateBounded<AmqpMethod>(1));
        var amqpMethod = await channel.Reader.ReadAsync(cancellationToken);
        return (T)amqpMethod;
    }
}