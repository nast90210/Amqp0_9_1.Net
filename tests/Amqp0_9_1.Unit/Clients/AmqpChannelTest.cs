using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Clients;
using Amqp0_9_1.Constants;
using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Channel;
using Amqp0_9_1.Methods.Exchange;
using Amqp0_9_1.Methods.Queue;
using Amqp0_9_1.Utilities;
using AutoFixture;
using Moq;

namespace Amqp0_9_1.Unit.Clients;

public class AmqpChannelTest
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IAmqpProcessor> _mockProcessor = new();

    [Fact]
    public async Task Create_ShouldSendChannelOpenAndReceiveChannelOpenOk_WhenCalled()
    {
        var channelId = _fixture.Create<ushort>();
        var cancellationToken = CancellationToken.None;
        var channelOpenOk = new ChannelOpenOk(GetChannelOpenOkPayload());

        _mockProcessor.Setup(x => x.WriteMethodAsync(It.IsAny<ChannelOpen>(), channelId, cancellationToken))
            .Returns(Task.CompletedTask);

        _mockProcessor.Setup(x => x.ReadMethodAsync<ChannelOpenOk>(cancellationToken))
            .ReturnsAsync(channelOpenOk);

        var channel = CreateAmqpChannelWithMock(channelId);

        await channel.Create(cancellationToken);

        _mockProcessor.Verify(x => x.WriteMethodAsync(It.IsAny<ChannelOpen>(), channelId, cancellationToken), Times.Once);
        _mockProcessor.Verify(x => x.ReadMethodAsync<ChannelOpenOk>(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ExchangeDeclareAsync_ShouldSendExchangeDeclare_AndReturnExchange()
    {
        var channelId = _fixture.Create<ushort>();
        var cancellationToken = CancellationToken.None;
        var exchangeName = _fixture.Create<string>();
        const ExchangeType exchangeType = ExchangeType.Fanout;

        _mockProcessor
            .Setup(x => x.WriteMethodAsync(It.IsAny<ExchangeDeclare>(), channelId, cancellationToken))
            .Returns(Task.CompletedTask);

        var channel = CreateAmqpChannelWithMock(channelId);

        var exchange = await channel.ExchangeDeclareAsync(exchangeName, exchangeType, cancellationToken);

        Assert.NotNull(exchange);
        _mockProcessor.Verify(
            x => x.WriteMethodAsync(It.IsAny<ExchangeDeclare>(), channelId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task QueueDeclareAsync_ShouldSendQueueDeclare_AndReturnQueue()
    {
        var channelId = _fixture.Create<ushort>();
        var cancellationToken = CancellationToken.None;
        var queueName = _fixture.Create<string>();

        _mockProcessor
            .Setup(x => x.WriteMethodAsync(It.IsAny<QueueDeclare>(), channelId, cancellationToken))
            .Returns(Task.CompletedTask);

        var channel = CreateAmqpChannelWithMock(channelId);

        var queue = await channel.QueueDeclareAsync(queueName, cancellationToken);

        Assert.NotNull(queue);
        _mockProcessor.Verify(
            x => x.WriteMethodAsync(It.IsAny<QueueDeclare>(), channelId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CloseAsync_ShouldSendChannelClose_AndReceiveChannelCloseOk_ReturnTrue()
    {
        var channelId = _fixture.Create<ushort>();
        var cancellationToken = CancellationToken.None;
        const ushort replyCode = 200;
        const string replyText = "OK";
        const ushort classId = 0;
        const ushort methodId = 0;

        _mockProcessor
            .Setup(x => x.WriteMethodAsync(It.IsAny<ChannelClose>(), channelId, cancellationToken))
            .Returns(Task.CompletedTask);

        _mockProcessor
            .Setup(x => x.ReadMethodAsync<ChannelCloseOk>(cancellationToken))
            .ReturnsAsync(new ChannelCloseOk());

        var channel = CreateAmqpChannelWithMock(channelId);

        var result = await channel.CloseAsync(replyCode, replyText, classId, methodId, cancellationToken);

        Assert.True(result);
        _mockProcessor.Verify(
            x => x.WriteMethodAsync(It.IsAny<ChannelClose>(), channelId, cancellationToken),
            Times.Once);
        _mockProcessor.Verify(
            x => x.ReadMethodAsync<ChannelCloseOk>(cancellationToken),
            Times.Once);
    }

    private AmqpChannel CreateAmqpChannelWithMock(ushort channelId)
    {
        var ctor = typeof(AmqpChannel).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null,
            types: [typeof(ushort), typeof(IAmqpProcessor)],
            modifiers: null);

        return ctor == null
            ? throw new InvalidOperationException("AmqpChannel internal constructor not found.")
            : (AmqpChannel)ctor.Invoke([channelId, _mockProcessor.Object]);
    }

    private static ReadOnlyMemory<byte> GetChannelOpenOkPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.ShortString(""));
        return buffer.WrittenMemory;
    }
}