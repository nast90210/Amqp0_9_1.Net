
using System.Buffers;

namespace Amqp0_9_1.Utilities
{
    public sealed class MemoryBuffer : IDisposable
    {
        private const int DefaultBufferCapacity = 256;
        private IMemoryOwner<byte> _rentedMemoryOwner;
        private Memory<byte> _internalMemory;
        private int _written;

        public MemoryBuffer(int initialBufferCapacity = DefaultBufferCapacity)
        {
            _rentedMemoryOwner = MemoryPool<byte>.Shared.Rent(initialBufferCapacity);
            _internalMemory = _rentedMemoryOwner.Memory;
        }

        public ReadOnlyMemory<byte> WrittenMemory
        {
            get
            {
                var result = new Memory<byte>(new byte[_written]);
                _rentedMemoryOwner.Memory.Slice(0, _written).CopyTo(result);
                return result;
            }
        }

        public int Length => _written;

        public void Write(ReadOnlyMemory<byte> value)
        {
            if(!value.TryCopyTo(_internalMemory))
            {
                WriteMultiplication(value);
            }

            Advance(value.Length);
        }

        public void Write(byte value)
        {
            Write(new Memory<byte>([value]));
        }

        private void WriteMultiplication(ReadOnlyMemory<byte> value)
        {
            var requiredSize = Math.Max(_internalMemory.Length * 2, value.Length + _internalMemory.Length);
            var newMemoryOwner = MemoryPool<byte>.Shared.Rent(requiredSize);
            _rentedMemoryOwner.Memory.CopyTo(newMemoryOwner.Memory);
            _rentedMemoryOwner.Dispose();
            _rentedMemoryOwner = newMemoryOwner;
            _internalMemory = _rentedMemoryOwner.Memory.Slice(_written);
            Write(value);
        }

        private void Advance(int count)
        {
            _internalMemory = _internalMemory.Slice(count);
            _written += count;
        }

        public void Dispose()
        {
            _rentedMemoryOwner.Memory.Span.Clear();
            _rentedMemoryOwner.Dispose();
        }
    }
}