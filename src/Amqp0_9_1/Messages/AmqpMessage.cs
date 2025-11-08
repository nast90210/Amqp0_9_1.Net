using Amqp0_9_1.Methods.Basic;

namespace Amqp0_9_1.Messages
{
    public sealed class AmqpMessage
    {
        internal BasicDeliver Method { get; set; } = null!;
        internal ContentHeader Header { get; set; } = null!;
        public string? Body { get; set; }

        public string ConsumerTag => Method.ConsumerTag;
        public ulong DeliveryTag => Method.DeliveryTag;
        public bool Redelivered => Method.Redelivered;
        public HeaderProperties Properties => Header.Properties;
    }
}