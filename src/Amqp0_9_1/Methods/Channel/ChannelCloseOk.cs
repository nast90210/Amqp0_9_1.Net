using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods.Channel
{
    internal sealed class ChannelCloseOk : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Channel;
        internal override ushort MethodId => ChannelMethodId.CloseOk;

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            throw new NotImplementedException();
        }
    }
}