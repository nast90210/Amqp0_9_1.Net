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
        private readonly MessageBuilder messageBuilder = new();

        public IncomeMessageProcessor(ChannelReader<AmqpRawFrame> messageChannelReader)
        {
            _messageChannelReader = messageChannelReader;
        }

        internal async void ExecuteAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var messageRawFrame = await _messageChannelReader.ReadAsync(cancellationToken);

                ProcessRawMessage(messageRawFrame);

                if (messageBuilder.TryGetMessage(out AmqpMessage? message))
                {
                    if (message == null)
                        throw new InvalidOperationException("Message can't be null.");

                    var channel = _consumers.GetOrAdd(message.Method.ConsumerTag, Channel.CreateUnbounded<AmqpMessage>());
                    await channel.Writer.WriteAsync(message, cancellationToken);
                    messageBuilder.Clear();
                }
            }
        }

        private void ProcessRawMessage(AmqpRawFrame messageRawFrame)
        {
            switch (messageRawFrame.Type)
            {
                case AmqpFrameType.Method:
                    messageBuilder.ParseMethod(messageRawFrame);
                    break;
                case AmqpFrameType.Header:
                    messageBuilder.ParseHeader(messageRawFrame);
                    break;
                case AmqpFrameType.Body:
                    messageBuilder.ParseBody(messageRawFrame);
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