using System.Collections.Concurrent;
using Amqp0_9_1.Frames;
using Amqp0_9_1.Methods.Basic;
using Amqp0_9_1.Methods.Queue;

namespace Amqp0_9_1.Clients;

public sealed class AmqpQueue
{
    private readonly string _queueName;
    private readonly ushort _channelId;
    private readonly FrameReader _frameReader;
    private readonly FrameWriter _frameWriter;
    private readonly ConcurrentDictionary<string, object> consumers = new();

    internal AmqpQueue(string queueName, ushort channelId, FrameReader frameReader, FrameWriter frameWriter)
    {
        _queueName = queueName;
        _channelId = channelId;
        _frameReader = frameReader;
        _frameWriter = frameWriter;
    }

    internal async Task InternalDeclareAsync(CancellationToken cancellationToken)
    {
        await SendDeclareAsync(cancellationToken);
    }

    private async Task SendDeclareAsync(CancellationToken cancellationToken)
    {
        var queueDeclare = new QueueDeclare(_queueName);
        await _frameWriter.WriteMethodAsync(queueDeclare, _channelId, cancellationToken);
    }

    public async Task BindAsync(string exchangeName, string routingKey, CancellationToken cancellationToken = default)
    {
        await SendQueueBindAsync(exchangeName, routingKey, cancellationToken);
    }

    private async Task SendQueueBindAsync(string exchangeName, string routingKey, CancellationToken cancellationToken)
    {
        var queueBind = new QueueBind(_queueName, exchangeName, routingKey);
        await _frameWriter.WriteMethodAsync(queueBind, _channelId, cancellationToken);
    }

    public async Task ConsumeAsync(Func<string, Task> consumer, CancellationToken cancellationToken = default)
    {
        await SendConsumeAsync(cancellationToken);
        var basicConsumeOk = await ReceiveConsumeOkAsync(cancellationToken);
        consumers.TryAdd(basicConsumeOk.ConsumerTag, consumer);
    }

    private async Task<BasicConsumeOk> ReceiveConsumeOkAsync(CancellationToken cancellationToken)
    {
        var amqpMethod = await _frameReader.ReadMethodAsync(cancellationToken);

        if(amqpMethod.GetType() != typeof(BasicConsumeOk))
            throw new InvalidOperationException($"Amqp method is not valid - {amqpMethod.GetType()}");

        return (BasicConsumeOk)amqpMethod;
    }

    private async Task SendConsumeAsync(CancellationToken cancellationToken)
    {
        var basicConsume = new BasicConsume(_queueName);
        await _frameWriter.WriteMethodAsync(basicConsume, _channelId, cancellationToken);
    }
}