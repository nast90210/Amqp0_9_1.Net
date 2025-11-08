using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Messages
{
    internal sealed class ContentHeader
    {
        internal ushort ClassId => MethodClassId.Basic;
        internal ushort Weight => 0;
        internal ulong BodySize { get; set; }
        internal HeaderProperties Properties { get; set; } = null!;

        internal ContentHeader(ReadOnlyMemory<byte> payload)
        {
            var classId = AmqpDecoder.Short(ref payload);
            var weight = AmqpDecoder.Short(ref payload);

            if(classId != ClassId && weight != Weight)
                throw new InvalidCastException($"Payload can't be parse to {this}.");

            BodySize = AmqpDecoder.LongLong(ref payload);
            Properties = new HeaderProperties(payload);
        }
    }
}