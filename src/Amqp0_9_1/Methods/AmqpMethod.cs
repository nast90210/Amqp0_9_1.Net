namespace Amqp0_9_1.Methods
{
    internal abstract class AmqpMethod
    {
        internal abstract ushort ClassId { get; }
        internal abstract ushort MethodId { get; }

        internal abstract ReadOnlyMemory<byte> GetPayload();
    }
}