using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Channel
{
    internal sealed class ChannelClose : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Channel;
        internal override ushort MethodId => ChannelMethodId.Close;

        public ushort ReplyCode { get; }
        public string ReplyText { get; }
        public ushort ExceptionClassId { get; }
        public ushort ExceptionMethodId { get; }

        public ChannelClose(
            ushort replyCode,
            string replyText,
            ushort exceptionClassId,
            ushort exceptionMethodId)
        {
            ReplyCode = replyCode;
            ReplyText = replyText;
            ExceptionClassId = exceptionClassId;
            ExceptionMethodId = exceptionMethodId;
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            var buffer = new MemoryBuffer();
            buffer.Write(AmqpEncoder.Short(ReplyCode));
            buffer.Write(AmqpEncoder.ShortString(ReplyText));
            buffer.Write(AmqpEncoder.Short(ExceptionClassId));
            buffer.Write(AmqpEncoder.Short(ExceptionMethodId));
            return buffer.WrittenMemory;
        }
    }
}
