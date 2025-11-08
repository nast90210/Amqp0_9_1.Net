using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods.Basic
{
    internal sealed class BasicDeliver : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Basic;
        internal override ushort MethodId => BasicMethodId.Deliver;

        public string ConsumerTag { get; }
        public ulong DeliveryTag { get; }
        public bool Redelivered { get; }
        public string ExchangeName { get; }
        public string RoutingKey { get; }

        internal BasicDeliver(ReadOnlyMemory<byte> payload)
        {
            ConsumerTag = AmqpDecoder.ShortString(ref payload);
            DeliveryTag = AmqpDecoder.LongLong(ref payload);
            Redelivered = AmqpDecoder.Bool(ref payload);
            ExchangeName = AmqpDecoder.ShortString(ref payload);
            RoutingKey = AmqpDecoder.ShortString(ref payload);
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            throw new NotImplementedException();
        }
    }
}