using System.Net.Sockets;

namespace Amqp.Core.Transports
{
    public sealed class TcpTransport(string host, int port) : Transport
    {
        private readonly string _host = host;
        private readonly int _port = port;

        public override async Task<Stream> ConnectAsync(CancellationToken cancellationToken = default)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port).ConfigureAwait(false);
            _stream = _client.GetStream();
            return _stream;
        }
    }
}
