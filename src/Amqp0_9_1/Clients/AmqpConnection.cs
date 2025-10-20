using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using Amqp.Core.Primitives.SASL;
using Amqp.Core.Transports;
using Amqp0_9_1.Frames;
using Amqp0_9_1.Headers;
using Amqp0_9_1.Methods.Connection;
using Amqp0_9_1.Methods.Connection.Properties;

namespace Amqp0_9_1.Clients
{
    public sealed class AmqpConnection : IAsyncDisposable
    {
        private readonly Transport _transport;

        private readonly Pipe _incomePipe;
        private readonly Pipe _outcomePipe;

        private readonly FrameReader _frameReader;
        private readonly FrameWriter _frameWriter;

        private bool IsOpened; 

        public AmqpConnection(string host, int port)
        {
            Debug.WriteLine($"{this}: Connecting host: {host}.");
            Debug.WriteLine($"{this}: Connecting port: {port}.");

            //TODO: Add SslTransport
            _transport = new TcpTransport(host, port);

            _incomePipe = new Pipe();
            _outcomePipe = new Pipe();

            _frameReader = new FrameReader(_incomePipe.Reader);
            _frameWriter = new FrameWriter(_outcomePipe.Writer);
        }

        public async Task ConnectAsync(
            string username, 
            string password, 
            string virtualHost = "/", 
            CancellationToken cancellationToken = default)
        {
            var stream = await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _ = IncomePipeAsync(cancellationToken);
            _ = OutcomePipeAsync(cancellationToken);

            await ProcessHandshake(username, password, virtualHost, cancellationToken);
        }

        private async Task IncomePipeAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    var buffer = _incomePipe.Writer.GetMemory(8192);
                    int bytesRead = await _transport.ReceiveAsync(buffer, token).ConfigureAwait(false);

                    if (bytesRead == 0)
                        throw new IOException("Connection closed prematurely");

                    _incomePipe.Writer.Advance(bytesRead);
                    await _incomePipe.Writer.FlushAsync(token).ConfigureAwait(false);
                }
            }
            finally
            {
                await _incomePipe.Writer.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async Task OutcomePipeAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    var result = await _outcomePipe.Reader.ReadAsync(token).ConfigureAwait(false);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        await _transport.SendAsync(segment, token).ConfigureAwait(false);
                    }

                    _outcomePipe.Reader.AdvanceTo(buffer.End);
                }
            }
            finally
            {
                await _outcomePipe.Reader.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async Task ProcessHandshake(string username, string password, string virtualHost, CancellationToken cancellationToken)
        {
            await SendProtocolHeader(_outcomePipe.Writer, cancellationToken);
            _ = await ReceiveConnectionStartAsync(cancellationToken);
            await SendConnectionStartOkAsync(username, password, cancellationToken);
            //FIXME: Currently no ConnectionSecurity
            var tuneInfo = await ReceiveConnectionTuneAsync(cancellationToken);
            await SendConnectionTuneOkAsync(tuneInfo.ChannelMax, tuneInfo.FrameMax, tuneInfo.Heartbeat, cancellationToken);
            await SendConnectionOpenAsync(virtualHost, cancellationToken);
            IsOpened = await ReceiveConnectionOpenOkAsync(cancellationToken);
        }

        private async Task SendProtocolHeader(PipeWriter writer, CancellationToken cancellationToken)
        {
            var protocolHeader = ProtocolHeader.GetPayload();

            await writer.WriteAsync(protocolHeader, cancellationToken);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<ConnectionStart> ReceiveConnectionStartAsync(CancellationToken token = default)
        {
            var amqpMethod = await _frameReader.ReadMethodAsync(token).ConfigureAwait(false);

            if (amqpMethod.GetType() != typeof(ConnectionStart))
                throw new InvalidOperationException($"Amqp method is not valid - {amqpMethod.GetType()}");

            var connectionStart = (ConnectionStart)amqpMethod;

            Debug.WriteLine($"{this}: Amqp version: {connectionStart.VersionMajor}.{connectionStart.VersionMinor}.");
            Debug.WriteLine($"{this}: Mechanisms: {connectionStart.Mechanisms}.");
            Debug.WriteLine($"{this}: Local: {connectionStart.Locales}.");

            foreach (var prop in connectionStart.ServerProperties)
            {
                if (prop.Value is Dictionary<string, object> dictionary)
                {
                    foreach (var subProp in dictionary)
                    {
                        Debug.WriteLine($"{this}: {subProp.Key}: {subProp.Value}.");
                    }
                    continue;
                }

                Debug.WriteLine($"{this}: {prop.Key}: {prop.Value}.");
            }

            return connectionStart;
        }

        private async Task SendConnectionStartOkAsync(string username, string password, CancellationToken cancellationToken)
        {
            var clientProperties = new ConnectionStartOkProperties();

            //FIXME: Currently only PLAIN 
            var saslPlainResponse = new SaslPlainResponse(username, password);

            var connectionStartOk = new ConnectionStartOk(
                clientProperties,
                SaslMechanism.PLAIN,
                saslPlainResponse,
                CultureInfo.CurrentCulture.Name);

            await _frameWriter.WriteMethodAsync(connectionStartOk, cancellationToken: cancellationToken);
        }

        private async Task<ConnectionTune> ReceiveConnectionTuneAsync(CancellationToken cancellationToken)
        {
            var amqpMethod = await _frameReader.ReadMethodAsync(cancellationToken).ConfigureAwait(false);

            if (amqpMethod.GetType() != typeof(ConnectionTune))
                throw new InvalidOperationException($"Amqp method is not valid - {amqpMethod.GetType()}");

            var connectionTune = (ConnectionTune)amqpMethod;

            Debug.WriteLine($"{this}: ChannelMax {connectionTune.ChannelMax}.");
            Debug.WriteLine($"{this}: FrameMax {connectionTune.FrameMax}.");
            Debug.WriteLine($"{this}: Heartbeat {connectionTune.Heartbeat}.");

            return connectionTune;
        }

        private async Task SendConnectionTuneOkAsync(ushort channelMax, uint frameMax, ushort heartbeat, CancellationToken cancellationToken)
        {
            var connectionTuneOk = new ConnectionTuneOk(channelMax, frameMax, heartbeat);
            await _frameWriter.WriteMethodAsync(connectionTuneOk, cancellationToken: cancellationToken);
        }

        private async Task SendConnectionOpenAsync(string virtualHost, CancellationToken cancellationToken)
        {
            var connectionTuneOk = new ConnectionOpen(virtualHost);
            await _frameWriter.WriteMethodAsync(connectionTuneOk, cancellationToken: cancellationToken);
        }

        private async Task<bool> ReceiveConnectionOpenOkAsync(CancellationToken cancellationToken)
        {
            var amqpMethod = await _frameReader.ReadMethodAsync(cancellationToken).ConfigureAwait(false);

            if (amqpMethod.GetType() != typeof(ConnectionOpenOk))
                throw new InvalidOperationException($"Amqp method is not valid - {amqpMethod.GetType()}");

            return true;            
        }

        private async Task SendConnectionCloseAsync(
            ushort replyCode, 
            string replyText,
            ushort? exceptionClassId = null,
            ushort? exceptionMethodId = null,
            CancellationToken cancellationToken = default)
        {
            var connectionClose = new ConnectionClose(replyCode, replyText, exceptionClassId, exceptionMethodId);
            await _frameWriter.WriteMethodAsync(connectionClose, cancellationToken: cancellationToken);
        } 

        public async Task<AmqpChannel> CreateChannelAsync(ushort channelId, CancellationToken cancellationToken = default)
        {
            var channel = new AmqpChannel(channelId, _frameReader, _frameWriter);
            await channel.Create(cancellationToken);
            return channel;
        }

        public async ValueTask DisposeAsync()
        {
            if(IsOpened)
            {
                await SendConnectionCloseAsync(200, "Goodbye").ConfigureAwait(false);
            }
            
            await _incomePipe.Reader.CompleteAsync().ConfigureAwait(false);
            await _incomePipe.Writer.CompleteAsync().ConfigureAwait(false);

            await _outcomePipe.Reader.CompleteAsync().ConfigureAwait(false);
            await _outcomePipe.Writer.CompleteAsync().ConfigureAwait(false);

            _transport?.Dispose();
        }
    }
}
