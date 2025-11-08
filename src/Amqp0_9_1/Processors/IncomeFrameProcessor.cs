using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Channels;
using Amqp0_9_1.Primitives.Frames;
using Amqp0_9_1.Frames;

namespace Amqp0_9_1.Processors
{
    internal sealed class IncomeFrameProcessor
    {
        private static readonly ReadOnlyMemory<byte> messageMask = new byte[] { 0, 60, 0, 60 };

        private readonly PipeReader _incomePipeReader;
        private readonly ChannelWriter<AmqpRawFrame> _methodChannelWriter;
        private readonly ChannelWriter<AmqpRawFrame> _messageChannelWriter;

        public IncomeFrameProcessor(
            PipeReader incomeReader,
            ChannelWriter<AmqpRawFrame> methodChannelWriter,
            ChannelWriter<AmqpRawFrame> messageChannelWriter)
        {
            _incomePipeReader = incomeReader;
            _methodChannelWriter = methodChannelWriter;
            _messageChannelWriter = messageChannelWriter;
        }

        internal async void ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = await _incomePipeReader.ReadAsync(cancellationToken);
                    var buffer = result.Buffer;

                    while (FrameReader.TryParseFrame(ref buffer, out AmqpRawFrame? frame))
                    {
                        if (frame == null)
                            throw new ArgumentNullException(nameof(frame));

                        switch (frame.Type)
                        {
                            case AmqpFrameType.Method:
                                await ProcessMethodFrameAsync(frame, cancellationToken);
                                break;
                            case AmqpFrameType.Header:
                            case AmqpFrameType.Body:
                                await ProcessMessageFrameAsync(frame, cancellationToken);
                                break;
                            case AmqpFrameType.Heartbeat:
                                Debug.WriteLine($"{this}: Heartbeat received.");
                                break;
                            default:
                                throw new InvalidOperationException($"Invalid FrameType {frame.Type}");
                        }
                    }

                    _incomePipeReader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await _incomePipeReader.CompleteAsync();
            }
        }

        private async Task ProcessMethodFrameAsync(AmqpRawFrame frame, CancellationToken cancellationToken)
        {
            if(messageMask.Span.SequenceEqual(frame.Payload.Slice(0,4).Span))
            {
                await ProcessMessageFrameAsync(frame, cancellationToken);
                return;
            }

            await _methodChannelWriter.WriteAsync(frame, cancellationToken);
        }

        private async Task ProcessMessageFrameAsync(AmqpRawFrame frame, CancellationToken cancellationToken)
        {
            await _messageChannelWriter.WriteAsync(frame, cancellationToken);
        }
    }
}