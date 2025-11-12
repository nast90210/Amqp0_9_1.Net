using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods.Basic
{
    internal sealed class BasicConsumeOk : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Basic;
        internal override ushort MethodId => BasicMethodId.ConsumeOk;

        public string ConsumerTag { get; }

        internal BasicConsumeOk(ReadOnlyMemory<byte> payload)
        {
            ConsumerTag = AmqpDecoder.ShortString(ref payload);
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            throw new NotImplementedException();
        }
    }
}
