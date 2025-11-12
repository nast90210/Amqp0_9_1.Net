using System.Buffers;
using Amqp0_9_1.Primitives.Frames;
using Amqp0_9_1.Encoding;

namespace Amqp0_9_1.Frames
{
    internal static class FrameReader
    {
        internal static bool TryParseFrame(ref ReadOnlySequence<byte> buffer, out AmqpRawFrame? frame)
        {
            var position = buffer.PositionOf(AmqpRawFrame.End);

            if (position == null)
            {
                frame = null;
                return false;
            }

            var frameBuffer = buffer.Slice(0, position.Value);

            frame = frameBuffer.IsSingleSegment switch
            {
                true => InternalParseFrame(frameBuffer.First),
                _ => InternalParseFrame(frameBuffer.ToArray())
            };

            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            return true;
        }

        private static AmqpRawFrame InternalParseFrame(ReadOnlyMemory<byte> segment)
        {
            if (segment.Length < 7)
                throw new ArgumentException("Segment is too short to be a valid AMQP frame.");

            var type = AmqpDecoder.Octet(ref segment);
            var channel = AmqpDecoder.Short(ref segment);
            var size = (int)AmqpDecoder.Long(ref segment);

            if (size is < 0 or > 0xFFFFFF)
                throw new ArgumentException("Invalid payload size in AMQP frame.");

            if (segment.Length != size)
            {
                throw new ArgumentException(
                    $"Segment length ({segment.Length}) does not match the size indicated in the frame header ({size}).");
            }

            return new AmqpRawFrame
            {
                Type = type,
                Channel = channel,
                Size = size,
                Payload = segment
            };
        }
    }
}
