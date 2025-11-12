using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Clients;
using Amqp0_9_1.Encoding;
using Amqp0_9_1.Messages;
using Amqp0_9_1.Methods.Basic;
using Amqp0_9_1.Methods.Queue;
using Amqp0_9_1.Utilities;
using AutoFixture;
using Moq;

namespace Amqp0_9_1.Unit.Clients;

public class AmqpQueueTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IAmqpProcessor> _mockProcessor = new();

    [Fact]
    public async Task InternalDeclareAsync_ShouldSendQueueDeclare_WithQueueName()
    {
        var channelId = _fixture.Create<ushort>();
        var queueName = _fixture.Create<string>();
        var cancellationToken = CancellationToken.None;

        _mockProcessor
            .Setup(p => p.WriteMethodAsync(It.IsAny<QueueDeclare>(), channelId, cancellationToken))
            .Returns(Task.CompletedTask);

        var queue = CreateAmqpQueueWithMock(queueName, channelId);

        await queue.InternalDeclareAsync(cancellationToken);

        _mockProcessor.Verify(
            p => p.WriteMethodAsync(
                It.Is<QueueDeclare>(queueDeclare => queueDeclare.QueueName == queueName),
                channelId,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task BindAsync_ShouldSendQueueBind_WithExchangeAndRoutingKey()
    {
        var channelId = _fixture.Create<ushort>();
        var queueName = _fixture.Create<string>();
        var exchangeName = _fixture.Create<string>();
        var routingKey = _fixture.Create<string>();
        var cancellationToken = CancellationToken.None;

        _mockProcessor
            .Setup(p => p.WriteMethodAsync(It.IsAny<QueueBind>(), channelId, cancellationToken))
            .Returns(Task.CompletedTask);

        var queue = CreateAmqpQueueWithMock(queueName, channelId);

        await queue.BindAsync(exchangeName, routingKey, cancellationToken);

        _mockProcessor.Verify(
            p => p.WriteMethodAsync(
                It.Is<QueueBind>(queueBind =>
                    queueBind.QueueName == queueName &&
                    queueBind.ExchangeName == exchangeName &&
                    queueBind.RoutingKey == routingKey),
                channelId,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task AckAsync_ShouldSendBasicAck_WithDeliveryTagAndMultiple()
    {
        var channelId = _fixture.Create<ushort>();
        var queueName = _fixture.Create<string>();
        var ct = CancellationToken.None;
        var deliveryTag = _fixture.Create<ulong>();
        var multiple = _fixture.Create<bool>();

        _mockProcessor
            .Setup(p => p.WriteMethodAsync(It.IsAny<BasicAck>(), channelId, ct))
            .Returns(Task.CompletedTask);

        var queue = CreateAmqpQueueWithMock(queueName, channelId);

        await queue.AckAsync(deliveryTag, multiple, ct);

        _mockProcessor.Verify(
            p => p.WriteMethodAsync(
                It.Is<BasicAck>(basicAck =>
                    basicAck.DeliveryTag == deliveryTag &&
                    basicAck.Multiple == multiple),
                channelId,
                ct),
            Times.Once);
    }

    [Fact]
    public async Task NackAsync_ShouldSendBasicNack_WithDeliveryTagMultipleAndRequeue()
    {
        var channelId = _fixture.Create<ushort>();
        var queueName = _fixture.Create<string>();
        var ct = CancellationToken.None;
        var deliveryTag = _fixture.Create<ulong>();
        var multiple = _fixture.Create<bool>();
        var requeue = _fixture.Create<bool>();

        _mockProcessor
            .Setup(p => p.WriteMethodAsync(It.IsAny<BasicNack>(), channelId, ct))
            .Returns(Task.CompletedTask);

        var queue = CreateAmqpQueueWithMock(queueName, channelId);

        await queue.NackAsync(deliveryTag, multiple, requeue, ct);

        _mockProcessor.Verify(
            p => p.WriteMethodAsync(
                It.Is<BasicNack>(basicNack =>
                    basicNack.DeliveryTag == deliveryTag &&
                    basicNack.Multiple == multiple &&
                    basicNack.Requeue == requeue),
                channelId,
                ct),
            Times.Once);
    }

    [Fact]
    public async Task ConsumeAsync_ShouldThrowArgumentNullException_WhenConsumerIsNull()
    {
        var channelId = _fixture.Create<ushort>();
        var queueName = _fixture.Create<string>();
        var queue = CreateAmqpQueueWithMock(queueName, channelId);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            queue.ConsumeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ConsumeAsync_ShouldContinueProcessing_WhenConsumerThrowsException()
    {
        var channelId = _fixture.Create<ushort>();
        var queueName = _fixture.Create<string>();

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var consumerTag = _fixture.Create<string>();

        _mockProcessor
            .Setup(p => p.WriteMethodAsync(It.IsAny<BasicConsume>(), channelId, ct))
            .Returns(Task.CompletedTask);

        _mockProcessor
            .Setup(p => p.ReadMethodAsync<BasicConsumeOk>(ct))
            .ReturnsAsync(new BasicConsumeOk(GetBasicConsumeOkPayload()));

        var firstMessage = new AmqpMessage();

        _mockProcessor
            .SetupSequence(p => p.ConsumeMessageAsync(consumerTag, ct))
            .Returns(async () =>
            {
                await Task.Yield();
                return firstMessage;
            })
            .Returns(async () => await Task.FromCanceled<AmqpMessage>(ct));

        var queue = CreateAmqpQueueWithMock(queueName, channelId);

        var consumeTask = queue.ConsumeAsync(_ => throw new InvalidOperationException("Test exception"), ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => consumeTask);
    }

    private AmqpQueue CreateAmqpQueueWithMock(string queueName, ushort channelId)
    {
        var ctor = typeof(AmqpQueue).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null,
            types: [typeof(string), typeof(ushort), typeof(IAmqpProcessor)],
            modifiers: null);

        return ctor == null
            ? throw new InvalidOperationException("AmqpQueue internal constructor not found.")
            : (AmqpQueue)ctor.Invoke([queueName, channelId, _mockProcessor.Object]);
    }

    private static ReadOnlyMemory<byte> GetBasicConsumeOkPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.ShortString(""));
        return buffer.WrittenMemory;
    }
}
