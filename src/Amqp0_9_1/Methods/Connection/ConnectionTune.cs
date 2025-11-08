using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods.Connection;

internal sealed class ConnectionTune : AmqpMethod
{
    internal override ushort ClassId => MethodClassId.Connection;
    internal override ushort MethodId => ConnectionMethodId.Tune;

    public ushort ChannelMax { get; }
    public uint FrameMax { get; }
    public ushort Heartbeat { get; }

    public ConnectionTune(ReadOnlyMemory<byte> payload)
    {
        ChannelMax = AmqpDecoder.Short(ref payload);
        FrameMax = AmqpDecoder.Long(ref payload);
        Heartbeat = AmqpDecoder.Short(ref payload);
    }

    internal override ReadOnlyMemory<byte> GetPayload()
    {
        throw new NotImplementedException();
    }
}
