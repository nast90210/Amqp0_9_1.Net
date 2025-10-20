using System.Buffers;
using System.Diagnostics;
using Amqp.Core.Services;
using Amqp0_9_1.Encoding;

namespace Amqp0_9_1.Abstractions
{
    internal abstract class AmqpMethod
    {
        internal abstract ushort ClassId { get; }
        internal abstract ushort MethodId { get; }
        
        internal abstract ReadOnlySpan<byte> GetPayload();

        protected void Validate(ushort classId, ushort methodId)
        {   
            if(classId != ClassId && methodId != MethodId)
            {
                Debug.WriteLine($"{this}: ClassId {ClassId}, MethodId {MethodId}, classId {classId}, methodId {methodId}.");
                throw new InvalidCastException($"Cannot cast to {this} because ClassId or MethodId is not valid.");
            }
        }

        protected ArrayBuffer InitiateBuffer()
        {
            var buffer = new ArrayBuffer();
            buffer.Write(Amqp0_9_1Writer.EncodeShort(ClassId));
            buffer.Write(Amqp0_9_1Writer.EncodeShort(MethodId));
            return buffer;
        }
    }
}