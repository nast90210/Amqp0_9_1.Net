using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Basic
{
    internal sealed class BasicConsume : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Basic;
        internal override ushort MethodId => BasicMethodId.Consume;

        public ushort Reserved1 { get; } = 0;
        public string QueueName { get; }
        public string ConsumerTag { get; }
        public bool NoLocal { get; }
        public bool NoAck { get; }
        public bool Exclusive { get; }
        public bool NoWait { get;  }

        //TODO: Add Arguments init
        public Dictionary<string, object> Arguments { get; set; } = [];

        internal BasicConsume(
            string queueName,
            string? consumerTag = null,
            bool noLocal = false,
            bool noAck = false,
            bool exclusive = false,
            bool noWait = false)
        {
            QueueName = queueName;
            ConsumerTag = consumerTag ?? string.Empty;
            NoLocal = noLocal;
            NoAck = noAck;
            Exclusive = exclusive;
            NoWait = noWait;
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            using MemoryBuffer buffer = new();
            buffer.Write(AmqpEncoder.Short(ClassId));
            buffer.Write(AmqpEncoder.Short(MethodId));
            buffer.Write(AmqpEncoder.Short(Reserved1));
            buffer.Write(AmqpEncoder.ShortString(QueueName));
            buffer.Write(AmqpEncoder.ShortString(ConsumerTag));

            byte flags = 0;
            if (NoLocal) flags |= 1 << 0;
            if (NoAck) flags |= 1 << 1;
            if (Exclusive) flags |= 1 << 2;
            if (NoWait) flags |= 1 << 3;

            buffer.Write(flags);
            buffer.Write(AmqpEncoder.Table(Arguments));
            return buffer.WrittenMemory;
        }
    }
}
