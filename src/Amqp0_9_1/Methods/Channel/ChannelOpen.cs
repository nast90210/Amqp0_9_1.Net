using Amqp0_9_1.Encoding;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Channel
{
    internal sealed class ChannelOpen : AmqpMethod
    {
        internal override ushort ClassId => 20;
        internal override ushort MethodId => 10;

        public string Reserved1 { get; } = string.Empty;

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            using var buffer = new MemoryBuffer();
            buffer.Write(AmqpEncoder.Short(ClassId));
            buffer.Write(AmqpEncoder.Short(MethodId));
            buffer.Write(AmqpEncoder.ShortString(Reserved1));
            return buffer.WrittenMemory;
        }
    }
}
