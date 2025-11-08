using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Basic
{
    internal sealed class BasicNack : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Basic;
        internal override ushort MethodId => BasicMethodId.Nack;

        public ulong DeliveryTag { get; }
        public bool Multiple { get; }
        public bool Requeue { get; }

        public BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            DeliveryTag = deliveryTag;
            Multiple = multiple;
            Requeue = requeue;
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            using var buffer = new MemoryBuffer();
            buffer.Write(AmqpEncoder.Short(ClassId));
            buffer.Write(AmqpEncoder.Short(MethodId));
            buffer.Write(AmqpEncoder.LongLong(DeliveryTag));

            byte flags = 0;
            if (Multiple) flags |= 1 << 0;
            if (Requeue) flags |= 1 << 1;
            buffer.Write(flags);

            return buffer.WrittenMemory;
        }
    }
}
