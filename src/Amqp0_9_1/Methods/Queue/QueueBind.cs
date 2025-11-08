using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Queue;

internal sealed class QueueBind : AmqpMethod
{
    internal override ushort ClassId => MethodClassId.Queue;
    internal override ushort MethodId => QueueMethodId.Bind;

    public ushort Reserved1 { get; } = 0;
    public string QueueName { get; }
    public string ExchangeName { get; }
    public string RoutingKey { get; }
    public bool IsNoWait { get; }

    //TODO: Add Arguments init
    public Dictionary<string, object> Arguments { get; } = [];

    internal QueueBind(
        string queueName,
        string exchangeName,
        string routingKey,
        bool isNoWait = true)
    {
        QueueName = queueName;
        ExchangeName = exchangeName;
        RoutingKey = routingKey;
        IsNoWait = isNoWait;
    }

    internal override ReadOnlyMemory<byte> GetPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.Short(ClassId));
        buffer.Write(AmqpEncoder.Short(MethodId));
        buffer.Write(AmqpEncoder.Short(Reserved1));
        buffer.Write(AmqpEncoder.ShortString(QueueName));
        buffer.Write(AmqpEncoder.ShortString(ExchangeName));
        buffer.Write(AmqpEncoder.ShortString(RoutingKey));

        byte flags = 0;
        if (IsNoWait) flags |= 1 << 0;
        buffer.Write(flags);

        buffer.Write(AmqpEncoder.Table(Arguments));
        return buffer.WrittenMemory;
    }
}