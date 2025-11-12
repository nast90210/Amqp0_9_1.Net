using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Connection
{
    internal sealed class ConnectionOpen : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Connection;
        internal override ushort MethodId => ConnectionMethodId.Open;

        public string VirtualHost { get; }
        public string Reserved1 { get; } = string.Empty;
        public bool Reserved2 { get; } = false;

        public ConnectionOpen(string virtualHost)
        {
            VirtualHost = virtualHost;
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            using var buffer = new MemoryBuffer();
            buffer.Write(AmqpEncoder.Short(ClassId));
            buffer.Write(AmqpEncoder.Short(MethodId));
            buffer.Write(AmqpEncoder.ShortString(VirtualHost));
            buffer.Write(AmqpEncoder.ShortString(Reserved1));
            buffer.Write(AmqpEncoder.Bool(Reserved2));
            return buffer.WrittenMemory;
        }
    }
}
