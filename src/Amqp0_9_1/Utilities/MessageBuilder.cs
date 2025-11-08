using Amqp0_9_1.Primitives.Frames;
using Amqp0_9_1.Methods.Basic;
using Amqp0_9_1.Messages;
using Amqp0_9_1.Methods;

namespace Amqp0_9_1.Utilities
{
    internal sealed class MessageBuilder
    {
        private AmqpMessage? _message;
        private bool _isFullyReceived;
        private MemoryBuffer? _bodyBuffer;

        internal void ParseMethod(AmqpRawFrame methodRawFrame)
        {
            if(_message != null)
                throw new InvalidOperationException("Message already initiated.");

            _message = new AmqpMessage
            {
                Method = (BasicDeliver)MethodFactory.Create(methodRawFrame)
            };
        }

        internal void ParseHeader(AmqpRawFrame headerRawFrame)
        {
            if(_message == null)
                throw new InvalidOperationException("Message not initiated.");

            if (_message.Header != null)
                throw new InvalidOperationException("HeaderFrame already set.");

            _message.Header = new ContentHeader(headerRawFrame.Payload);

            if(_message.Header.BodySize != 0)
            {
                _bodyBuffer = new MemoryBuffer((int)_message.Header.BodySize);
            }
            else
            {
                _isFullyReceived = true;
            }
        }

        internal void ParseBody(AmqpRawFrame bodyRawFrame)
        {
            if(_message == null)
                throw new InvalidOperationException("Message not initiated.");

            if(_bodyBuffer == null)
                throw new InvalidOperationException("Internal Body Buffer wasn't initiated.");

            _bodyBuffer.Write(bodyRawFrame.Payload);

            if(_message.Header.BodySize == (ulong)_bodyBuffer.Length)
            {
                _isFullyReceived = true;
            }
        }

        internal bool TryGetMessage(out AmqpMessage? message)
        {
            if(_message == null || !_isFullyReceived)
            {
                message = null;
                return false;
            }

            if(_message.Header.BodySize != 0)
            {
                if(_bodyBuffer == null)
                    throw new InvalidOperationException("Internal Body Buffer wasn't initiated.");

                _message.Body = ContentDecoder.Decode(_message.Header.Properties.ContentEncoding, _bodyBuffer.WrittenMemory);
            }

            message = _message;
            return true;
        }

        internal void Clear()
        {
            _bodyBuffer?.Dispose();
            _isFullyReceived = false;
            _message = null;
        }
    }
}