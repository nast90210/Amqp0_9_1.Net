using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Amqp0_9_1.Primitives.Frames;
using Amqp0_9_1.Frames;
using Amqp0_9_1.Methods;
using Amqp0_9_1.Transports;
using Amqp0_9_1.Messages;

namespace Amqp0_9_1.Processors
{
    internal sealed class InternalAmqpProcessor : IDisposable
    {
        private readonly Transport _transport;
        private readonly Pipe _incomePipe = new();
        private readonly Pipe _outcomePipe = new();

        private readonly Channel<AmqpRawFrame> _methodChannel;
        private readonly Channel<AmqpRawFrame> _messageChannel;
        private readonly IncomeFrameProcessor _frameProcessor;
        private readonly IncomeMethodProcessor _methodProcessor;
        private readonly IncomeMessageProcessor _messageProcessor;

        public InternalAmqpProcessor(string host, int port)
        {
            _transport = new TcpTransport(host, port);

            _methodChannel = Channel.CreateUnbounded<AmqpRawFrame>();
            _messageChannel = Channel.CreateUnbounded<AmqpRawFrame>();

            _frameProcessor = new IncomeFrameProcessor(_incomePipe.Reader, _methodChannel.Writer, _messageChannel.Writer);
            _methodProcessor = new IncomeMethodProcessor(_methodChannel.Reader);
            _messageProcessor = new IncomeMessageProcessor(_messageChannel.Reader);
        }

        internal async Task ConnectServerAsync(CancellationToken cancellationToken = default)
        {
            await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

            OutcomeWriterAsync(cancellationToken);
            IncomeReaderAsync(cancellationToken);

            _frameProcessor.ExecuteAsync(cancellationToken);
            _methodProcessor.ExecuteAsync(cancellationToken);
            _messageProcessor.ExecuteAsync(cancellationToken);
        }

        private async void OutcomeWriterAsync(CancellationToken token)
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

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await _outcomePipe.Reader.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async void IncomeReaderAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    var buffer = _incomePipe.Writer.GetMemory(8192);

                    if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
                    {
                        int bytesRead = await _transport.ReceiveAsync(segment, token).ConfigureAwait(false);

                        Debug.WriteLine($"{this}: Read {bytesRead} bytes");

                        if (bytesRead == 0)
                            throw new IOException("Connection closed prematurely");

                        _incomePipe.Writer.Advance(bytesRead);
                    }
                    else
                    {
                        var tempBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                        int bytesRead = await _transport.ReceiveAsync(tempBuffer, token).ConfigureAwait(false);

                        Debug.WriteLine($"{this}: Read {bytesRead} bytes");

                        if (bytesRead == 0)
                            throw new IOException("Connection closed prematurely");

                        tempBuffer.AsMemory(0, bytesRead).CopyTo(buffer);
                        _incomePipe.Writer.Advance(bytesRead);
                        ArrayPool<byte>.Shared.Return(tempBuffer, true);
                    }

                    var result = await _incomePipe.Writer.FlushAsync(token).ConfigureAwait(false);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await _incomePipe.Writer.CompleteAsync().ConfigureAwait(false);
            }
        }

        internal async Task<T> ReadMethodAsync<T>(CancellationToken cancellationToken)
            where T : AmqpMethod
        {
            return await _methodProcessor.ReadAsync<T>(cancellationToken);
        }

        internal async Task<AmqpMessage> ConsumeMessageAsync(string consumerTag, CancellationToken cancellationToken)
        {
            return await _messageProcessor.ConsumeAsync(consumerTag, cancellationToken);
        }

        internal async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            await _outcomePipe.Writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            await _outcomePipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        internal async Task WriteMethodAsync(AmqpMethod amqpMethod, ushort channel = 0, CancellationToken cancellationToken = default)
        {
            await WriteAsync(FrameWriter.GetMethodPayload(amqpMethod, channel), cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _incomePipe.Reader.CompleteAsync().GetAwaiter().GetResult();
            _incomePipe.Writer.CompleteAsync().GetAwaiter().GetResult();

            _outcomePipe.Reader.CompleteAsync().GetAwaiter().GetResult();
            _outcomePipe.Writer.CompleteAsync().GetAwaiter().GetResult();
            _transport?.Dispose();
        }
    }
}