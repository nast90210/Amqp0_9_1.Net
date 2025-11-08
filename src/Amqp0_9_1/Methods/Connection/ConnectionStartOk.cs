using Amqp0_9_1.Primitives.SASL;
using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Connection.Properties;
using Amqp0_9_1.Utilities;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods.Connection;

internal sealed class ConnectionStartOk : AmqpMethod
{
    internal override ushort ClassId => MethodClassId.Connection;
    internal override ushort MethodId => ConnectionMethodId.StartOk;

    public ConnectionStartOkProperties ClientProperties { get; }
    public string Mechanism { get; }
    public SaslPlainResponse Response { get; }
    public string Locale { get; }

    internal ConnectionStartOk(
        ConnectionStartOkProperties clientProperties,
        string mechanism,
        SaslPlainResponse response,
        string locale)
    {
        ClientProperties = clientProperties;
        Mechanism = mechanism;
        Response = response;
        Locale = locale;
    }

    internal override ReadOnlyMemory<byte> GetPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.Short(ClassId));
        buffer.Write(AmqpEncoder.Short(MethodId));
        buffer.Write(AmqpEncoder.Table(ClientProperties.ToDictionary()));
        buffer.Write(AmqpEncoder.ShortString(Mechanism));
        buffer.Write(Response.GetPayload());
        buffer.Write(AmqpEncoder.ShortString(Locale));
        return buffer.WrittenMemory;
    }
}
