using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Amqp0_9_1.Transports
{
    public sealed class SslTransport : Transport
    {
        private readonly string _host;
        private readonly int _port;
        private readonly X509Certificate2? _clientCert;

        public SslTransport(string host, int port, X509Certificate2? clientCert = null)
        {
            _host = host;
            _port = port;
            _clientCert = clientCert;
        }

        public override async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port).ConfigureAwait(false);

            var sslStream = new SslStream(
                innerStream: _client.GetStream(),
                leaveInnerStreamOpen: false,
                //FIXME: Add server cert validation
                userCertificateValidationCallback: (_, __, ___, ____) => true,
                userCertificateSelectionCallback: null);

            var certs = _clientCert != null ? new X509Certificate2Collection { _clientCert } : null;

            await sslStream.AuthenticateAsClientAsync(_host, certs, SslProtocols.Tls, false).ConfigureAwait(false);
            _stream = sslStream;
        }
    }
}
