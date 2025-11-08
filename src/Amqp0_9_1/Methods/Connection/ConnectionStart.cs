using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods.Connection;

internal sealed class ConnectionStart : AmqpMethod
{
    internal override ushort ClassId => MethodClassId.Connection;
    internal override ushort MethodId => ConnectionMethodId.Start;

    public ushort VersionMajor { get; }
    public ushort VersionMinor { get; }
    public Dictionary<string, object> ServerProperties { get; } = [];
    public string Mechanisms { get; } = string.Empty;
    public string Locales { get; } = string.Empty;

    internal ConnectionStart(ReadOnlyMemory<byte> payload)
    {
        VersionMajor = AmqpDecoder.Octet(ref payload);
        VersionMinor = AmqpDecoder.Octet(ref payload);
        ServerProperties = AmqpDecoder.Table(ref payload);
        Mechanisms = AmqpDecoder.LongString(ref payload);
        Locales = AmqpDecoder.LongString(ref payload);
    }

    internal override ReadOnlyMemory<byte> GetPayload()
    {
        throw new NotImplementedException();
    }
}
