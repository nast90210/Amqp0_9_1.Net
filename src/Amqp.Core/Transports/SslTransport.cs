using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Amqp.Core.Transports
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

        public override async Task<Stream> ConnectAsync(CancellationToken cancellationToken = default)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port).ConfigureAwait(false);

            var sslStream = new SslStream(
                innerStream: _client.GetStream(),
                leaveInnerStreamOpen: false,
                //FIXME: Add server cert validation
                userCertificateValidationCallback: (sender, certificate, chain, sslPolicyErrors) => true,
                userCertificateSelectionCallback: null);

            var certs = _clientCert != null ? new X509Certificate2Collection { _clientCert } : null;
            var protocol = SslProtocols.Tls;
            await sslStream.AuthenticateAsClientAsync(_host, certs, protocol, false).ConfigureAwait(false);
            _stream = sslStream;

            return sslStream;
        }
    }
}
