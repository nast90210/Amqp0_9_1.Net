using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Constants;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Methods.Connection
{
    internal sealed class ConnectionClose : AmqpMethod
    {
        internal override ushort ClassId => MethodClassId.Connection;
        internal override ushort MethodId => ConnectionMethodId.ConnectionClose;

        public ushort ReplyCode { get; }
        public string ReplyText { get; }
        public ushort ExceptionClassId { get; }
        public ushort ExceptionMethodId { get; }

        public ConnectionClose(
            ushort replyCode,
            string replyText,
            ushort? exceptionClassId,
            ushort? exceptionMethodId)
        {
            ReplyCode = replyCode;
            ReplyText = replyText;
            ExceptionClassId = exceptionClassId ?? 0;
            ExceptionMethodId = exceptionMethodId ?? 0;
        }

        public ConnectionClose(
            ReadOnlyMemory<byte> payload)
        {
            ReplyCode = AmqpDecoder.Short(ref payload);;
            ReplyText = AmqpDecoder.ShortString(ref payload);;
            ExceptionClassId = AmqpDecoder.Short(ref payload);;
            ExceptionMethodId = AmqpDecoder.Short(ref payload);;
        }

        internal override ReadOnlyMemory<byte> GetPayload()
        {
            using var buffer = new MemoryBuffer();
            buffer.Write(AmqpEncoder.Short(ClassId));
            buffer.Write(AmqpEncoder.Short(MethodId));
            buffer.Write(AmqpEncoder.Short(ReplyCode));
            buffer.Write(AmqpEncoder.ShortString(ReplyText));
            buffer.Write(AmqpEncoder.Short(ExceptionClassId));
            buffer.Write(AmqpEncoder.Short(ExceptionMethodId));
            return buffer.WrittenMemory;
        }
    }
}
