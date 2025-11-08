using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Exchange;

internal sealed class ExchangeDeclare : AmqpMethod
{
    internal override ushort ClassId => MethodClassId.Exchange;
    internal override ushort MethodId => ExchangeMethodId.Declare;

    public ushort Reserved1 { get; } = 0;
    public string ExchangeName { get; }
    public string ExchangeType { get; }
    public bool IsPassive { get; }
    public bool IsDurable { get; }
    public bool IsAutoDelete { get; }
    public bool IsInternal { get; }
    public bool IsNoWait { get; }
    public Dictionary<string, object> Arguments { get; } = [];

    public ExchangeDeclare(
        string exchangeName,
        string exchangeType,
        bool isPassive = false,
        bool isDurable = true,
        bool isAutoDelete = false,
        bool isInternal = false,
        bool isNoWait = true)
    {
        ExchangeName = exchangeName;
        ExchangeType = exchangeType;
        IsPassive = isPassive;
        IsDurable = isDurable;
        IsAutoDelete = isAutoDelete;
        IsInternal = isInternal;
        IsNoWait = isNoWait;
    }

    internal override ReadOnlyMemory<byte> GetPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.Short(ClassId));
        buffer.Write(AmqpEncoder.Short(MethodId));
        buffer.Write(AmqpEncoder.Short(Reserved1));
        buffer.Write(AmqpEncoder.ShortString(ExchangeName));
        buffer.Write(AmqpEncoder.ShortString(ExchangeType));

        byte flags = 0;
        if (IsPassive) flags |= 1 << 0;
        if (IsDurable) flags |= 1 << 1;
        if (IsAutoDelete) flags |= 1 << 2;
        if (IsInternal) flags |= 1 << 3;
        if (IsNoWait) flags |= 1 << 4;
        buffer.Write(flags);

        buffer.Write(AmqpEncoder.Table(Arguments));
        return buffer.WrittenMemory;
    }
}