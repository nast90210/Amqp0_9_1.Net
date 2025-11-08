using Amqp0_9_1.Methods.Exchange;
using Amqp0_9_1.Processors;

namespace Amqp0_9_1.Clients
{
    public sealed class AmqpExchange
    {
        private readonly ushort _channelId;
        private readonly InternalAmqpProcessor _amqpDispatcher;

        internal AmqpExchange(ushort channelId, InternalAmqpProcessor amqpDispatcher)
        {
            _channelId = channelId;
            _amqpDispatcher = amqpDispatcher;
        }

        internal async Task InternalDeclareAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
        {
            await SendDeclareAsync(exchangeName, exchangeType, cancellationToken);
        }

        private async Task SendDeclareAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
        {
            //TODO: Add Arguments init
            var exchangeDeclare = new ExchangeDeclare(exchangeName, exchangeType);
            await _amqpDispatcher.WriteMethodAsync(exchangeDeclare, _channelId, cancellationToken);
        }
    }
}