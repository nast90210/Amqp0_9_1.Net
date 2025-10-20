using System.Buffers;
using System.IO.Pipelines;
using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods;

namespace Amqp0_9_1.Frames;

internal sealed class FrameReader(PipeReader reader)
{
    private readonly PipeReader _reader = reader;

    public async Task<byte[]> ReadAsync(CancellationToken token = default)
    {
        byte[] header = await ReadExactAsync(1 + 2 + 4, token).ConfigureAwait(false);
        int offset = 0;

        byte frameType = header[offset++];

        return frameType switch
        {
            FrameType.Method => await ReadMethodFrameAsync(header, offset, token),
            _ => throw new InvalidOperationException(
                                $"Unexpected frame type {frameType}, expected METHOD ({FrameType.Method})."),
        };
    }

    public async Task<AmqpMethod> ReadMethodAsync(CancellationToken cancellationToken)
    {
        byte[] header = await ReadExactAsync(1 + 2 + 4, cancellationToken).ConfigureAwait(false);
        int offset = 0;

        byte frameType = header[offset++];

        if(frameType != FrameType.Method)
            throw new InvalidOperationException($"Invalid method frame type - {frameType}");

        ushort channel = Amqp0_9_1Reader.DecodeShort(header, ref offset);

        uint size = Amqp0_9_1Reader.DecodeLong(header, ref offset);
        offset += 4;

        byte[] payload = await ReadExactAsync(size, cancellationToken).ConfigureAwait(false);

        byte[] end = await ReadExactAsync(1, cancellationToken).ConfigureAwait(false);
        if (end[0] != FrameType.End)
            throw new InvalidOperationException("Invalid frame end byte.");

        offset = 0;
        var classId = Amqp0_9_1Reader.DecodeShort(payload, ref offset);
        var methodId = Amqp0_9_1Reader.DecodeShort(payload, ref offset);

        return MethodFactory.Create(classId, methodId, payload);
    }

    private async Task<byte[]> ReadMethodFrameAsync(byte[] header, int offset, CancellationToken token)
    {
        ushort channel = Amqp0_9_1Reader.DecodeShort(header, ref offset);

        uint size = Amqp0_9_1Reader.DecodeLong(header, ref offset);
        offset += 4;

        byte[] payload = await ReadExactAsync(size, token).ConfigureAwait(false);

        byte[] end = await ReadExactAsync(1, token).ConfigureAwait(false);
        if (end[0] != FrameType.End)
            throw new InvalidOperationException("Invalid frame end byte.");

        return payload;
    }

    private async Task<byte[]> ReadExactAsync(uint count, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var result = await _reader.ReadAsync(token).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (buffer.Length >= count)
            {
                var slice = buffer.Slice(0, count);
                byte[] data = new byte[count];
                slice.CopyTo(data);

                _reader.AdvanceTo(slice.End);
                return data;
            }

            if (result.IsCompleted)
                throw new EndOfStreamException("Stream ended before required bytes were read.");

            _reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }
}
