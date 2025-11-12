using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Methods.Channel;
using Amqp0_9_1.Constants;

namespace Amqp0_9_1.Clients
{
    public sealed class AmqpChannel : IDisposable
    {
        private readonly ushort _channelId;
        private readonly IAmqpProcessor _amqpProcessor;
        private bool _isOpened;

        internal AmqpChannel(ushort channelId, IAmqpProcessor amqpProcessor)
        {
            _channelId = channelId;
            _amqpProcessor = amqpProcessor;
        }

        internal async Task Create(CancellationToken cancellationToken)
        {
            var channelOpen = new ChannelOpen();
            await _amqpProcessor.WriteMethodAsync(channelOpen, _channelId, cancellationToken);
            await _amqpProcessor.ReadMethodAsync<ChannelOpenOk>(cancellationToken);
            _isOpened = true;
        }

        public async Task<AmqpExchange> ExchangeDeclareAsync(
            string exchangeName,
            ExchangeType exchangeType,
            CancellationToken cancellationToken = default)
        {
            var exchange = new AmqpExchange(_channelId, _amqpProcessor);
            await exchange.InternalDeclareAsync(exchangeName, exchangeType.ToString().ToLowerInvariant(), cancellationToken);
            return exchange;
        }

        public async Task<AmqpQueue> QueueDeclareAsync(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            var queue = new AmqpQueue(queueName, _channelId, _amqpProcessor);
            await queue.InternalDeclareAsync(cancellationToken);
            return queue;
        }

        public async Task<bool> CloseAsync(
            ushort replyCode,
            string replyText,
            ushort exceptionClassId = 0,
            ushort exceptionMethodId = 0,
            CancellationToken cancellationToken = default)
        {
            var channelClose = new ChannelClose(replyCode, replyText, exceptionClassId, exceptionMethodId);
            await _amqpProcessor.WriteMethodAsync(channelClose, _channelId, cancellationToken);
            _ = await _amqpProcessor.ReadMethodAsync<ChannelCloseOk>(cancellationToken);
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_isOpened)
            {
                await CloseAsync(200, "Channel disposing");
                _isOpened = false;
            }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
