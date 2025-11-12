using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Clients;
using Amqp0_9_1.Methods.Exchange;
using AutoFixture;
using Moq;

namespace Amqp0_9_1.Unit.Clients;

public class AmqpExchangeTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IAmqpProcessor> _mockProcessor = new();

    [Fact]
    public async Task InternalDeclareAsync_ShouldSendExchangeDeclare_WithGivenNameAndType()
    {
        var channelId = _fixture.Create<ushort>();
        var exchangeName = _fixture.Create<string>();
        var exchangeType = _fixture.Create<string>();
        var cancellationToken = CancellationToken.None;

        _mockProcessor
            .Setup(p => p.WriteMethodAsync(It.IsAny<ExchangeDeclare>(), channelId, cancellationToken))
            .Returns(Task.CompletedTask);

        var exchange = CreateAmqpExchangeWithMock(channelId);

        await exchange.InternalDeclareAsync(exchangeName, exchangeType, cancellationToken);

        _mockProcessor.Verify(
            p => p.WriteMethodAsync(
                It.Is<ExchangeDeclare>(exchangeDeclare => 
                    exchangeDeclare.ExchangeName == exchangeName &&
                    exchangeDeclare.ExchangeType == exchangeType),
                channelId,
                cancellationToken),
            Times.Once);
    }

    private AmqpExchange CreateAmqpExchangeWithMock(ushort channelId)
    {
        var ctor = typeof(AmqpExchange).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null,
            types: [typeof(ushort), typeof(IAmqpProcessor)],
            modifiers: null);

        return ctor == null
            ? throw new InvalidOperationException("AmqpExchange internal constructor not found.")
            : (AmqpExchange)ctor.Invoke([channelId, _mockProcessor.Object]);
    }
}
