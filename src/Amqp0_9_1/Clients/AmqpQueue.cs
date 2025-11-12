using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Messages;
using Amqp0_9_1.Methods.Basic;
using Amqp0_9_1.Methods.Queue;

namespace Amqp0_9_1.Clients
{
    public sealed class AmqpQueue
    {
        private readonly string _queueName;
        private readonly ushort _channelId;
        private readonly IAmqpProcessor _amqpProcessor;

        internal AmqpQueue(string queueName, ushort channelId, IAmqpProcessor amqpProcessor)
        {
            _queueName = queueName;
            _channelId = channelId;
            _amqpProcessor = amqpProcessor;
        }

        internal async Task InternalDeclareAsync(CancellationToken cancellationToken)
        {
            var queueDeclare = new QueueDeclare(_queueName);
            await _amqpProcessor.WriteMethodAsync(queueDeclare, _channelId, cancellationToken);
        }

        public async Task BindAsync(string exchangeName, string routingKey, CancellationToken cancellationToken = default)
        {
            var queueBind = new QueueBind(_queueName, exchangeName, routingKey);
            await _amqpProcessor.WriteMethodAsync(queueBind, _channelId, cancellationToken);
        }

        public async Task ConsumeAsync(Func<AmqpMessage, Task> consumer, CancellationToken cancellationToken = default)
        {
            var basicConsume = new BasicConsume(_queueName);
            await _amqpProcessor.WriteMethodAsync(basicConsume, _channelId, cancellationToken);

            var basicConsumeOk = await _amqpProcessor.ReadMethodAsync<BasicConsumeOk>(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _amqpProcessor.ConsumeMessageAsync(basicConsumeOk.ConsumerTag, cancellationToken);
                await consumer.Invoke(message);
            }
        }

        public async Task AckAsync(
            ulong deliveryTag,
            bool multiple = false,
            CancellationToken cancellationToken = default)
        {
            var basicAck = new BasicAck(deliveryTag, multiple);
            await _amqpProcessor.WriteMethodAsync(basicAck, _channelId, cancellationToken);
        }

        public async Task NackAsync(
            ulong deliveryTag,
            bool multiple = false,
            bool requeue = true,
            CancellationToken cancellationToken = default)
        {
            var basicNack = new BasicNack(deliveryTag, multiple, requeue);
            await _amqpProcessor.WriteMethodAsync(basicNack, _channelId, cancellationToken);
        }
    }
}