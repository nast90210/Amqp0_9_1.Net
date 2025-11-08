namespace Amqp0_9_1.Primitives.Frames
{
    internal class AmqpRawFrame
    {
        internal byte Type { get; set; }
        internal ushort Channel { get; set; }
        internal int Size { get; set; }
        internal ReadOnlyMemory<byte> Payload { get; set; }
        internal static byte End => 0xCE;
    }
}