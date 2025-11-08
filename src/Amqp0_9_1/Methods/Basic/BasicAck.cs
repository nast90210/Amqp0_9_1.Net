using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Basic
{
    internal sealed class BasicAck : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Basic;
        internal override ushort MethodId => BasicMethodId.Ack;

        public ulong DeliveryTag { get; }
        public bool Multiple { get; }

        internal BasicAck(ulong deliveryTag, bool multiple)
        {
            DeliveryTag = deliveryTag;
            Multiple = multiple;
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            using MemoryBuffer buffer = new();
            buffer.Write(AmqpEncoder.Short(ClassId));
            buffer.Write(AmqpEncoder.Short(MethodId));
            buffer.Write(AmqpEncoder.LongLong(DeliveryTag));

            byte flags = 0;
            if (Multiple)
            {
                flags |= 1 << 0;
            }

            buffer.Write(flags);

            return buffer.WrittenMemory;
        }
    }
}
