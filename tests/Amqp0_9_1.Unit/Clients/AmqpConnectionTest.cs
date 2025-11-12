using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Clients;
using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Channel;
using Amqp0_9_1.Methods.Connection;
using Amqp0_9_1.Utilities;
using AutoFixture;
using Moq;

namespace Amqp0_9_1.Unit.Clients;

public class AmqpConnectionTest
{
    private readonly IFixture _fixture;
    private readonly Mock<IAmqpProcessor> _mockProcessor;
    private readonly string _host;
    private readonly int _port;

    public AmqpConnectionTest()
    {
        _fixture = new Fixture();
        _mockProcessor = new Mock<IAmqpProcessor>();
        _host = _fixture.Create<string>();
        _port = _fixture.Create<int>();
    }

    [Fact]
    public async Task ConnectAsync_ShouldCallAllProcessHandshakeMethods_WhenConnectionEstablished()
    {
        // Arrange
        var username = _fixture.Create<string>();
        var password = _fixture.Create<string>();
        var virtualHost = _fixture.Create<string>();
        var cancellationToken = CancellationToken.None;

        var connectionStart = new ConnectionStart(GetConnectionStartPayload());
        var connectionTune = new ConnectionTune(GetConnectionTunePayload());
        var connectionOpenOk = new ConnectionOpenOk(GetConnectionOpenOkPayload());

        _mockProcessor.Setup(x => x.StartProcessingAsync(cancellationToken))
            .Returns(Task.CompletedTask);
        _mockProcessor.Setup(x => x.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _mockProcessor.Setup(x => x.ReadMethodAsync<ConnectionStart>(cancellationToken))
            .ReturnsAsync(connectionStart);
        _mockProcessor.Setup(x => x.ReadMethodAsync<ConnectionTune>(cancellationToken))
            .ReturnsAsync(connectionTune);
        _mockProcessor.Setup(x => x.ReadMethodAsync<ConnectionOpenOk>(cancellationToken))
            .ReturnsAsync(connectionOpenOk);
        _mockProcessor.Setup(x =>
                x.WriteMethodAsync(It.IsAny<ConnectionStartOk>(), It.IsAny<ushort>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _mockProcessor.Setup(x =>
                x.WriteMethodAsync(It.IsAny<ConnectionTuneOk>(), It.IsAny<ushort>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _mockProcessor.Setup(x => x.WriteMethodAsync(It.IsAny<ConnectionOpen>(), It.IsAny<ushort>(), cancellationToken))
            .Returns(Task.CompletedTask);

        var connection = CreateAmqpConnectionWithMock();
        await connection.ConnectAsync(username, password, virtualHost, cancellationToken);

        // Verify ConnectServerAsync
        _mockProcessor.Verify(x => x.StartProcessingAsync(cancellationToken), Times.Once);

        // Verify SendProtocolHeader
        _mockProcessor.Verify(x => x.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), cancellationToken), Times.Once);

        // Verify ReceiveConnectionStartAsync
        _mockProcessor.Verify(x => x.ReadMethodAsync<ConnectionStart>(cancellationToken), Times.Once);

        // Verify SendConnectionStartOkAsync
        _mockProcessor.Verify(
            x => x.WriteMethodAsync(It.IsAny<ConnectionStartOk>(), It.IsAny<ushort>(), cancellationToken), Times.Once);

        // Verify ReceiveConnectionTuneAsync
        _mockProcessor.Verify(x => x.ReadMethodAsync<ConnectionTune>(cancellationToken), Times.Once);

        // Verify SendConnectionTuneOkAsync
        _mockProcessor.Verify(
            x => x.WriteMethodAsync(It.IsAny<ConnectionTuneOk>(), It.IsAny<ushort>(), cancellationToken), Times.Once);

        // Verify SendConnectionOpenAsync
        _mockProcessor.Verify(
            x => x.WriteMethodAsync(It.IsAny<ConnectionOpen>(), It.IsAny<ushort>(), cancellationToken), Times.Once);

        // Verify ReceiveConnectionOpenOkAsync
        _mockProcessor.Verify(x => x.ReadMethodAsync<ConnectionOpenOk>(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ConnectionCloseAsync_ShouldReadConnectionCloseMethod_WhenCalled()
    {
        var cancellationToken = CancellationToken.None;
        var connectionClose = new ConnectionClose(GetConnectionClosePayload());

        _mockProcessor.Setup(x => x.ReadMethodAsync<ConnectionClose>(cancellationToken))
            .ReturnsAsync(connectionClose);

        var connection = CreateAmqpConnectionWithMock();

        var result = await connection.ConnectionCloseAsync(cancellationToken);

        Assert.True(result);
        _mockProcessor.Verify(x => x.ReadMethodAsync<ConnectionClose>(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateChannelAsync_ShouldCreateChannelAndCallChannelOpenMethods_WhenCalled()
    {
        var channelId = _fixture.Create<ushort>();
        var cancellationToken = CancellationToken.None;
        var channelOpenOk = new ChannelOpenOk(GetChannelOpenOkPayload());

        _mockProcessor.Setup(x => x.WriteMethodAsync(It.IsAny<ChannelOpen>(), channelId, cancellationToken))
            .Returns(Task.CompletedTask);

        _mockProcessor.Setup(x => x.ReadMethodAsync<ChannelOpenOk>(cancellationToken))
            .ReturnsAsync(channelOpenOk);

        var connection = CreateAmqpConnectionWithMock();

        var channel = await connection.CreateChannelAsync(channelId, cancellationToken);

        Assert.NotNull(channel);
        _mockProcessor.Verify(x => x.WriteMethodAsync(It.IsAny<ChannelOpen>(), channelId, cancellationToken),
            Times.Once);
        _mockProcessor.Verify(x => x.ReadMethodAsync<ChannelOpenOk>(cancellationToken), Times.Once);
    }

    private AmqpConnection CreateAmqpConnectionWithMock()
    {
        var connection = new AmqpConnection(_host, _port);

        var processorField = typeof(AmqpConnection).GetField("_amqpProcessor",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        processorField?.SetValue(connection, _mockProcessor.Object);

        return connection;
    }

    private static ReadOnlyMemory<byte> GetConnectionStartPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(0);
        buffer.Write(9);
        buffer.Write(AmqpEncoder.Table(new Dictionary<string, object>()));
        buffer.Write(AmqpEncoder.LongString("PLAIN"));
        buffer.Write(AmqpEncoder.LongString("en_US"));
        return buffer.WrittenMemory;
    }

    private static ReadOnlyMemory<byte> GetConnectionTunePayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.Short(0));
        buffer.Write(AmqpEncoder.Long(0));
        buffer.Write(AmqpEncoder.Short(0));
        return buffer.WrittenMemory;
    }

    private static ReadOnlyMemory<byte> GetConnectionOpenOkPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.ShortString(""));
        return buffer.WrittenMemory;
    }

    private static ReadOnlyMemory<byte> GetConnectionClosePayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.Short(200));
        buffer.Write(AmqpEncoder.ShortString("OK"));
        buffer.Write(AmqpEncoder.Short(0));
        buffer.Write(AmqpEncoder.Short(0));
        return buffer.WrittenMemory;
    }

    private static ReadOnlyMemory<byte> GetChannelOpenOkPayload()
    {
        using var buffer = new MemoryBuffer();
        buffer.Write(AmqpEncoder.ShortString(""));
        return buffer.WrittenMemory;
    }
}
