namespace Amqp0_9_1.Primitives.Frames
{
    internal sealed class AmqpRawMessage
    {
        internal AmqpRawFrame MethodFrame { get; set; } = null!;
        internal AmqpRawFrame? HeaderFrame { get; set; }
        internal ICollection<AmqpRawFrame> BodyFrames { get; set; } = [];
    }
}