using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods.Channel
{
    internal sealed class ChannelOpenOk : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Channel;
        internal override ushort MethodId => ChannelMethodId.OpenOk;

        public string? Reserved1 { get; set; }

        internal ChannelOpenOk(ReadOnlyMemory<byte> payload)
        {
            Reserved1 = AmqpDecoder.ShortString(ref payload);
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            throw new NotImplementedException();
        }
    }
}
