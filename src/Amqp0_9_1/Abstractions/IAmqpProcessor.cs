using Amqp0_9_1.Messages;
using Amqp0_9_1.Methods;

namespace Amqp0_9_1.Abstractions;

internal interface IAmqpProcessor : IDisposable
{
    Task StartProcessingAsync(CancellationToken cancellationToken = default);
    Task<T> ReadMethodAsync<T>(CancellationToken cancellationToken) where T : AmqpMethod;
    Task<AmqpMessage> ConsumeMessageAsync(string consumerTag, CancellationToken cancellationToken);
    Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    Task WriteMethodAsync(AmqpMethod amqpMethod, ushort channel = 0, CancellationToken cancellationToken = default);
}