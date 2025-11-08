using Amqp0_9_1.Primitives.Frames;
using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Frames
{
    internal static class FrameWriter
    {
        internal static ReadOnlyMemory<byte> GetMethodPayload(AmqpMethod method, ushort channel = 0)
        {
            var payload = method.GetPayload();

            using var buffer = new MemoryBuffer();
            buffer.Write(AmqpFrameType.Method);
            buffer.Write(AmqpEncoder.Short(channel));
            buffer.Write(AmqpEncoder.Long((uint)payload.Length));
            buffer.Write(payload);
            buffer.Write(AmqpRawFrame.End);
            return buffer.WrittenMemory;
        }
    }
}