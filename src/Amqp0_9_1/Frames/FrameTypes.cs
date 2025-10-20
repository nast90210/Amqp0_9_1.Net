namespace Amqp0_9_1.Frames
{
    public static class FrameType
    {
        public const byte Method = 1;
        public const byte Header = 2;
        public const byte Body = 2;
        public const byte End = 0xCE;
    }
}
