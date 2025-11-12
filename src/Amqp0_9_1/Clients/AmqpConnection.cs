using System.Diagnostics;
using System.Globalization;
using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Primitives.SASL;
using Amqp0_9_1.Methods.Connection;
using Amqp0_9_1.Methods.Connection.Properties;
using Amqp0_9_1.Processors;

namespace Amqp0_9_1.Clients
{
    public sealed class AmqpConnection : IDisposable
    {
        private readonly IAmqpProcessor _amqpProcessor;
        private bool _isOpened;

        public AmqpConnection(string host, int port)
        {
            Debug.WriteLine($"{this}: Connecting host: {host}.");
            Debug.WriteLine($"{this}: Connecting port: {port}.");

            _amqpProcessor = new GeneralAmqpProcessor(host, port);
        }

        public async Task ConnectAsync(
            string username,
            string password,
            string virtualHost = "/",
            CancellationToken cancellationToken = default)
        {
            await _amqpProcessor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

            await ProcessHandshake(username, password, virtualHost, cancellationToken);
        }

        private async Task ProcessHandshake(string username, string password, string virtualHost, CancellationToken cancellationToken)
        {
            await SendProtocolHeader(cancellationToken);
            _ = await ReceiveConnectionStartAsync(cancellationToken);
            await SendConnectionStartOkAsync(username, password, cancellationToken);
            // //FIXME: Currently no ConnectionSecurity
            var tuneInfo = await ReceiveConnectionTuneAsync(cancellationToken);
            await SendConnectionTuneOkAsync(tuneInfo.ChannelMax, tuneInfo.FrameMax, tuneInfo.Heartbeat, cancellationToken);
            await SendConnectionOpenAsync(virtualHost, cancellationToken);
            _isOpened = await ReceiveConnectionOpenOkAsync(cancellationToken);
        }

        //TODO: In future - move up to basic class
        private async Task SendProtocolHeader(CancellationToken cancellationToken)
        {
            var protocolHeader = "AMQP\x00\x00\x09\x01"u8.ToArray();
            await _amqpProcessor.WriteAsync(protocolHeader, cancellationToken);
        }

        private async Task<ConnectionStart> ReceiveConnectionStartAsync(CancellationToken cancellationToken = default)
        {
            var connectionStart = await _amqpProcessor.ReadMethodAsync<ConnectionStart>(cancellationToken).ConfigureAwait(false);

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

            await _amqpProcessor.WriteMethodAsync(connectionStartOk, cancellationToken: cancellationToken);
        }

        private async Task<ConnectionTune> ReceiveConnectionTuneAsync(CancellationToken cancellationToken)
        {
            var connectionTune = await _amqpProcessor.ReadMethodAsync<ConnectionTune>(cancellationToken).ConfigureAwait(false);

            Debug.WriteLine($"{this}: ChannelMax {connectionTune.ChannelMax}.");
            Debug.WriteLine($"{this}: FrameMax {connectionTune.FrameMax}.");
            Debug.WriteLine($"{this}: Heartbeat {connectionTune.Heartbeat}.");

            return connectionTune;
        }

        private async Task SendConnectionTuneOkAsync(ushort channelMax, uint frameMax, ushort heartbeat, CancellationToken cancellationToken)
        {
            var connectionTuneOk = new ConnectionTuneOk(channelMax, frameMax, heartbeat);
            await _amqpProcessor.WriteMethodAsync(connectionTuneOk, cancellationToken: cancellationToken);
        }

        private async Task SendConnectionOpenAsync(string virtualHost, CancellationToken cancellationToken)
        {
            var connectionTuneOk = new ConnectionOpen(virtualHost);
            await _amqpProcessor.WriteMethodAsync(connectionTuneOk, cancellationToken: cancellationToken);
        }

        private async Task<bool> ReceiveConnectionOpenOkAsync(CancellationToken cancellationToken)
        {
            _ = await _amqpProcessor.ReadMethodAsync<ConnectionOpenOk>(cancellationToken).ConfigureAwait(false);
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
            await _amqpProcessor.WriteMethodAsync(connectionClose, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ConnectionCloseAsync(CancellationToken cancellationToken = default)
        {
            await _amqpProcessor.ReadMethodAsync<ConnectionClose>(cancellationToken);
            return true;
        }

        public async Task<AmqpChannel> CreateChannelAsync(ushort channelId, CancellationToken cancellationToken = default)
        {
            var channel = new AmqpChannel(channelId, _amqpProcessor);
            await channel.Create(cancellationToken);
            return channel;
        }

        public async ValueTask DisposeAsync()
        {
            if (_isOpened)
            {
                await SendConnectionCloseAsync(200, "Goodbye").ConfigureAwait(false);
                //TODO: add awaiting ConnectionCloseOk
                _isOpened = false;
            }

            _amqpProcessor.Dispose();
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
