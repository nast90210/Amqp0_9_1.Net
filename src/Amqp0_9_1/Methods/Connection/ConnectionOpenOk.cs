using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods.Connection;

internal sealed class ConnectionOpenOk : AmqpMethod
{
    internal override ushort ClassId => MethodClassId.Connection;
    internal override ushort MethodId => ConnectionMethodId.OpenOk;

    public string KnownHosts { get; }

    internal ConnectionOpenOk(ReadOnlyMemory<byte> payload)
    {
        KnownHosts = AmqpDecoder.ShortString(ref payload);
    }

    internal override ReadOnlyMemory<byte> GetPayload()
    {
        throw new NotImplementedException();
    }
}
