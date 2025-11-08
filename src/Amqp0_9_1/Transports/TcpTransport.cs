using System.Net.Sockets;

namespace Amqp0_9_1.Transports
{
    public sealed class TcpTransport : Transport
    {
        private readonly string _host;
        private readonly int _port;

        public TcpTransport(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public override async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port).ConfigureAwait(false);
            _stream = _client.GetStream();
        }
    }
}
