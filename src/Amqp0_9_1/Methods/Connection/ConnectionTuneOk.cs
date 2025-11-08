using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Connection;

internal sealed class ConnectionTuneOk : AmqpMethod
{
    internal override ushort ClassId => MethodClassId.Connection;
    internal override ushort MethodId => ConnectionMethodId.TuneOk;

    public ushort ChannelMax { get; }
    public uint FrameMax { get; }
    public ushort Heartbeat { get; }

    public ConnectionTuneOk(ushort channelMax, uint frameMax, ushort heartbeat)
    {
        ChannelMax = channelMax;
        FrameMax = frameMax;
        Heartbeat = heartbeat;
    }

    internal override ReadOnlyMemory<byte> GetPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.Short(ClassId));
        buffer.Write(AmqpEncoder.Short(MethodId));
        buffer.Write(AmqpEncoder.Short(ChannelMax));
        buffer.Write(AmqpEncoder.Long(FrameMax));
        buffer.Write(AmqpEncoder.Short(Heartbeat));
        return buffer.WrittenMemory;
    }
}
