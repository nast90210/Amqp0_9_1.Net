using Amqp0_9_1.Frames;
using Amqp0_9_1.Methods.Exchange;

namespace Amqp0_9_1.Clients
{
    public sealed class AmqpExchange
    {
        private readonly ushort _channelId;
        private readonly FrameReader _frameReader;
        private readonly FrameWriter _frameWriter;

        internal AmqpExchange(ushort channelId, FrameReader frameReader, FrameWriter frameWriter)
        {
            _channelId = channelId;
            _frameReader = frameReader;
            _frameWriter = frameWriter;
        }

        internal async Task InternalDeclareAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
        {
            await SendDeclareAsync(exchangeName, exchangeType, cancellationToken);
        }

        private async Task SendDeclareAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
        {
            var exchangeDeclare = new ExchangeDeclare(exchangeName, exchangeType);
            await _frameWriter.WriteMethodAsync(exchangeDeclare, _channelId, cancellationToken);
        }
    }
}