using System.Collections.Concurrent;
using System.Threading.Channels;
using Amqp0_9_1.Primitives.Frames;
using Amqp0_9_1.Utilities;
using Amqp0_9_1.Messages;

namespace Amqp0_9_1.Processors
{
    internal sealed class IncomeMessageProcessor
    {
        private readonly ChannelReader<AmqpRawFrame> _messageChannelReader;
        private readonly ConcurrentDictionary<string, Channel<AmqpMessage>> _consumers = new();
        private readonly MessageBuilder _messageBuilder = new();

        public IncomeMessageProcessor(ChannelReader<AmqpRawFrame> messageChannelReader)
        {
            _messageChannelReader = messageChannelReader;
        }

        internal async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    var messageRawFrame = await _messageChannelReader.ReadAsync(cancellationToken);

                    ProcessRawMessage(messageRawFrame);

                    if (!_messageBuilder.TryGetMessage(out var message)) continue;
                    
                    if (message == null)
                        throw new InvalidOperationException("Message can't be null.");

                    var channel = _consumers.GetOrAdd(message.Method.ConsumerTag,
                        Channel.CreateUnbounded<AmqpMessage>());
                    await channel.Writer.WriteAsync(message, cancellationToken);
                    _messageBuilder.Clear();
                }
            }
            finally
            {
            }
        }

        private void ProcessRawMessage(AmqpRawFrame messageRawFrame)
        {
            switch (messageRawFrame.Type)
            {
                case AmqpFrameType.Method:
                    _messageBuilder.ParseMethod(messageRawFrame);
                    break;
                case AmqpFrameType.Header:
                    _messageBuilder.ParseHeader(messageRawFrame);
                    break;
                case AmqpFrameType.Body:
                    _messageBuilder.ParseBody(messageRawFrame);
                    break;
            }
        }

        internal async Task<AmqpMessage> ConsumeAsync(string consumerTag, CancellationToken cancellationToken)
        {
            var channel = _consumers.GetOrAdd(consumerTag, Channel.CreateUnbounded<AmqpMessage>());
            return await channel.Reader.ReadAsync(cancellationToken);
        }
    }
}