using Amqp0_9_1.Encoding;

namespace Amqp0_9_1.Messages
{
    public sealed class HeaderProperties
    {
        public string? ContentType { get; }
        public string? ContentEncoding { get; }
        public Dictionary<string, object> Headers { get; } = [];
        public byte? DeliveryMode { get; private set; }
        public byte? Priority { get; private set; }
        public string? CorrelationId { get; }
        public string? ReplyTo { get; }
        public string? Expiration { get; }
        public string? MessageId { get; }
        public DateTime? Timestamp { get; private set; }
        public string? Type { get; }
        public string? UserId { get; }
        public string? AppId { get; }
        internal string? Reserved { get; }

        internal HeaderProperties(ReadOnlyMemory<byte> payload)
        {
            //TODO: Implement dynamic properties flag
            var flag = AmqpDecoder.Short(ref payload);

            for (var index = 15; index >= 0; index--)
            {
                var isExist = (flag >> index & 1) != 0;
                if (!isExist)
                {
                    continue;
                }

                switch (index)
                {
                    case 15:
                        ContentType = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 14:
                        ContentEncoding = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 13:
                        Headers = AmqpDecoder.Table(ref payload);
                        break;
                    case 12:
                        DeliveryMode = AmqpDecoder.Octet(ref payload);
                        break;
                    case 11:
                        Priority = AmqpDecoder.Octet(ref payload);
                        break;
                    case 10:
                        CorrelationId = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 9:
                        ReplyTo = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 8:
                        Expiration = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 7:
                        MessageId = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 6:
                        Timestamp = AmqpDecoder.Timestamp(ref payload);
                        break;
                    case 5:
                        Type = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 4:
                        UserId = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 3:
                        AppId = AmqpDecoder.ShortString(ref payload);
                        break;
                    case 2:
                    case 1:
                    case 0:
                        Reserved = AmqpDecoder.ShortString(ref payload);
                        break;
                }
            }
        }
    }
}