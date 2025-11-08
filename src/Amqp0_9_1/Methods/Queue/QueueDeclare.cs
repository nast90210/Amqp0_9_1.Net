using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Queue;

internal sealed class QueueDeclare : AmqpMethod
{
    internal override ushort ClassId => MethodClassId.Queue;
    internal override ushort MethodId => QueueMethodId.Declare;

    public ushort Reserved1 { get; } = 0;
    public string QueueName { get; }
    public bool IsPassive { get; }
    public bool IsDurable { get; }
    public bool IsExclusive { get; }
    public bool IsAutoDelete { get; }
    public bool IsNoWait { get; }

    //TODO: Add Arguments init
    public Dictionary<string, object> Arguments { get; } = [];

    internal QueueDeclare(
        string queueName,
        bool isPassive = false,
        bool isDurable = true,
        bool isExclusive = false,
        bool isAutoDelete = false,
        bool isNoWait = true)
    {
        QueueName = queueName;
        IsPassive = isPassive;
        IsDurable = isDurable;
        IsExclusive = isExclusive;
        IsAutoDelete = isAutoDelete;
        IsNoWait = isNoWait;
    }

    internal override ReadOnlyMemory<byte> GetPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.Short(ClassId));
        buffer.Write(AmqpEncoder.Short(MethodId));
        buffer.Write(AmqpEncoder.Short(Reserved1));
        buffer.Write(AmqpEncoder.ShortString(QueueName));

        byte flags = 0;
        if (IsPassive) flags |= 1 << 0;
        if (IsDurable) flags |= 1 << 1;
        if (IsExclusive) flags |= 1 << 2;
        if (IsAutoDelete) flags |= 1 << 3;
        if (IsNoWait) flags |= 1 << 4;
        buffer.Write(flags);

        buffer.Write(AmqpEncoder.Table(Arguments));
        return buffer.WrittenMemory;
    }
}