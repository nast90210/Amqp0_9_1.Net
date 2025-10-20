using System.Net.Sockets;

namespace Amqp.Core.Transports
{
    public abstract class Transport : IDisposable
    {
        internal TcpClient? _client;
        internal Stream? _stream;

        public abstract Task<Stream> ConnectAsync(CancellationToken cancellationToken = default);

        public async Task SendAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Transport is not connected.");
            }

            await _stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Transport is not connected.");
            }

            return await _stream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _client?.Close();
        }
    }
}
