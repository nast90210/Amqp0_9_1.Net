using Amqp0_9_1.Frames;
using Amqp0_9_1.Methods.Channel;

namespace Amqp0_9_1.Clients;

public sealed class AmqpChannel : IAsyncDisposable
{
    private readonly ushort _channelId;
    private readonly FrameReader _frameReader;
    private readonly FrameWriter _frameWriter;

    internal AmqpChannel(ushort channelId, FrameReader frameReader, FrameWriter frameWriter)
    {
        _channelId = channelId;
        _frameReader = frameReader;
        _frameWriter = frameWriter;
    }

    internal async Task Create(CancellationToken cancellationToken)
    {
        await SendChannelOpenAsync(cancellationToken);
        await ReceiveChannelOpenOkAsync(cancellationToken);
    }

    private async Task SendChannelOpenAsync(CancellationToken cancellationToken)
    {
        var channelOpen = new ChannelOpen();
        await _frameWriter.WriteMethodAsync(channelOpen, _channelId, cancellationToken);
    }

    private async Task ReceiveChannelOpenOkAsync(CancellationToken cancellationToken)
    {
        var amqpMethod = await _frameReader.ReadMethodAsync(cancellationToken);

        if(amqpMethod.GetType() != typeof(ChannelOpenOk))
            throw new InvalidOperationException($"Amqp method is not valid - {amqpMethod.GetType()}");
    }

    public async Task<AmqpExchange> InitiateExchangeAsync(
        string exchangeName, 
        string exchangeType, 
        CancellationToken cancellationToken = default)
    {
        var exchange = new AmqpExchange(_channelId, _frameReader, _frameWriter);
        await exchange.InternalDeclareAsync(exchangeName, exchangeType, cancellationToken);
        return exchange;
    }

    public async Task<AmqpQueue> InitiateQueueAsync(
        string queueName,
        CancellationToken cancellationToken = default)
    {
        var queue = new AmqpQueue(queueName, _channelId, _frameReader, _frameWriter);
        await queue.InternalDeclareAsync(cancellationToken);
        return queue;
    }

    // // ---------- Publishing ----------
    // public async Task BasicPublishAsync(string exchange,
    //                                     string routingKey,
    //                                     byte[] body,
    //                                     bool mandatory = false,
    //                                     bool immediate = false,
    //                                     CancellationToken ct = default)
    // {
    //     var basicPublish = new BasicPublish
    //     {
    //         // Reserved1 = 0,
    //         Exchange = exchange,
    //         RoutingKey = routingKey,
    //         Mandatory = mandatory,
    //         Immediate = immediate
    //     };
    //     await _connection.SendMethodAsync(_channelId, basicPublish, ct).ConfigureAwait(false);

    //     // Header frame (content‑type, delivery‑mode etc.) – minimal example
    //     var header = new BasicContentHeader
    //     {
    //         BodySize = (ulong)body.Length,
    //         PropertyFlags = 0 // no properties set
    //     };

    //     var headerFrame = FrameWriter.BuildHeaderFrame(_channelId, header.ClassId, header.Weight, header.BodySize, new Dictionary<string, object>());
    //     await _connection!._transport!.SendAsync(headerFrame, 0, headerFrame.Length, ct)
    //                      .ConfigureAwait(false);

    //     // Body frame(s)
    //     var bodyFrame = FrameWriter.BuildBodyFrame(_channelId, body);
    //     await _connection._transport.SendAsync(bodyFrame, 0, bodyFrame.Length, ct)
    //                      .ConfigureAwait(false);
    // }

    // // ---------- Consuming ----------
    // public async Task<BasicConsumer> BasicConsumeAsync(string queue,
    //                                                    string consumerTag = "",
    //                                                    bool noLocal = false,
    //                                                    bool noAck = false,
    //                                                    bool exclusive = false,
    //                                                    Dictionary<string, object>? arguments = null,
    //                                                    CancellationToken ct = default)
    // {
    //     var consume = new BasicConsume
    //     {
    //         Queue = queue,
    //         ConsumerTag = consumerTag,
    //         NoLocal = noLocal,
    //         NoAck = noAck,
    //         // Exclusive = exclusive,
    //         Arguments = arguments ?? new Dictionary<string, object>()
    //     };
    //     await _connection.SendMethodAsync(_channelId, consume, ct).ConfigureAwait(false);
    //     var ok = await _connection.WaitForMethodAsync<BasicConsumeOk>(_channelId, ct).ConfigureAwait(false);
    //     return new BasicConsumer(this, ok.ConsumerTag);
    // }

    // // ---------- Acknowledgement ----------
    // public Task BasicAckAsync(ulong deliveryTag, bool multiple = false, CancellationToken ct = default)
    // {
    //     var ack = new BasicAck
    //     {
    //         DeliveryTag = deliveryTag,
    //         Multiple = multiple
    //     };
    //     return _connection.SendMethodAsync(_channelId, ack, ct);
    // }

    // public Task BasicNackAsync(ulong deliveryTag, bool multiple = false, bool requeue = true, CancellationToken ct = default)
    // {
    //     var ack = new BasicNack
    //     {
    //         DeliveryTag = deliveryTag,
    //         Multiple = multiple,
    //         Requeue = requeue
    //     };
    //     return _connection.SendMethodAsync(_channelId, ack, ct);
    // }

    public async ValueTask DisposeAsync()
    {
        var channelClose = new ChannelClose(200, "Channel closed");
        await _frameWriter.WriteMethodAsync(channelClose, _channelId).ConfigureAwait(false);
    }
}
