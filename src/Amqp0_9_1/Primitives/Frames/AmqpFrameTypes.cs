namespace Amqp0_9_1.Primitives.Frames
{
    internal static class AmqpFrameType
    {
        internal const byte Method = 1;
        internal const byte Header = 2;
        internal const byte Body = 3;
        internal const byte Heartbeat = 8;
    }
}
