using System.Buffers;
using System.IO.Pipelines;
using Amqp0_9_1.Abstractions;
using Amqp0_9_1.Encoding;

namespace Amqp0_9_1.Frames
{
    internal sealed class FrameWriter(PipeWriter writer)
    {
        private readonly PipeWriter _writer = writer;
        
        public async Task WriteMethodAsync(AmqpMethod method, ushort channel = 0, CancellationToken cancellationToken = default)
        {
            var payload = method.GetPayload();

            await _writer.WriteAsync(new[] { FrameType.Method });
            _writer.Write(Amqp0_9_1Writer.EncodeShort(channel));
            _writer.Write(Amqp0_9_1Writer.EncodeLong((uint)payload.Length));
            _writer.Write(payload);
            await _writer.WriteAsync(new[] { FrameType.End });

            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        // -----------------------------------------------------------------
        // HEADER FRAME
        // -----------------------------------------------------------------
        /// <summary>
        /// Builds a HEADER frame.
        /// </summary>
        /// <param name="channel">Channel number.</param>
        /// <param name="classId">Class id of the method (e.g., 60 for basic).</param>
        /// <param name="weight">Weight – always 0 for AMQP 0‑9‑1.</param>
        /// <param name="bodySize">Total size of the message body in bytes.</param>
        /// <param name="properties">Dictionary of standard AMQP properties.</param>
        // public static byte[] BuildHeaderFrame(
        //     ushort channel,
        //     uint classId,
        //     uint weight,
        //     ulong bodySize,
        //     IDictionary<string, object> properties)
        // {
        //     // ---- property list (binary) ----
        //     byte[] propertyBytes = EncodeProperties(properties);

        //     // ---- payload (class‑id, weight, body‑size, property‑flags, properties) ----
        //     using var payloadStream = new MemoryStream();
        //     var payloadWriter = new Amqp0_9_1Writer(payloadStream);
        //     payloadWriter.WriteUInt(classId);                     // class‑id
        //     payloadWriter.WriteUInt(weight);            // weight (0)
        //     payloadWriter.WriteULong(bodySize);               // body size
        //     payloadWriter.WriteInt(propertyBytes.Length); // property‑flags length (simplified)
        //     payloadWriter.Write(propertyBytes);                  // property list
        //     byte[] payloadBytes = payloadStream.ToArray();

        //     // ---- full frame ----
        //     using var frameStream = new MemoryStream();
        //     var frameWriter = new Amqp0_9_1Writer(frameStream);
        //     frameWriter.WriteByte(HeaderFrameType);           // type = HEADER
        //     frameWriter.WriteUShort(channel);
        //     frameWriter.WriteInt(payloadBytes.Length); // size
        //     frameWriter.Write(payloadBytes);
        //     frameWriter.WriteByte(FrameEnd);
        //     return frameStream.ToArray();
        // }

        // -----------------------------------------------------------------
        // BODY FRAME
        // -----------------------------------------------------------------
        /// <summary>
        /// Builds a BODY frame. The body may be split across several frames;
        /// each call encodes one chunk.
        /// </summary>
        // public static byte[] BuildBodyFrame(ushort channel, byte[] bodyChunk)
        // {
        //     using var frameStream = new MemoryStream();
        //     var frameWriter = new Amqp0_9_1Writer(frameStream);
        //     frameWriter.WriteByte(BodyFrameType);            // type = BODY
        //     frameWriter.WriteUShort(channel);
        //     frameWriter.WriteInt(bodyChunk.Length);    // size of this chunk
        //     frameWriter.Write(bodyChunk);
        //     frameWriter.WriteByte(FrameEnd);
        //     return frameStream.ToArray();
        // }

        // -----------------------------------------------------------------
        // PRIVATE HELPERS
        // -----------------------------------------------------------------
        /// <summary>
        /// Encodes a dictionary of AMQP properties into the binary format
        /// expected by a HEADER frame. Only a subset of common properties is
        /// demonstrated; extend as needed.
        /// </summary>
        // private static byte[] EncodeProperties(IDictionary<string, object> props)
        // {
        //     using var memoryStream = new MemoryStream();
        //     var writer = new Amqp0_9_1Writer(memoryStream);

        //     // Property flags – each bit indicates presence of a property.
        //     // For simplicity we build a 16‑bit flag field.
        //     ushort flags = 0;
        //     const ushort ContentTypeFlag = 1 << 15;
        //     const ushort ContentEncodingFlag = 1 << 14;
        //     const ushort DeliveryModeFlag = 1 << 13;
        //     const ushort PriorityFlag = 1 << 12;
        //     const ushort CorrelationIdFlag = 1 << 11;
        //     const ushort ReplyToFlag = 1 << 10;
        //     const ushort ExpirationFlag = 1 << 9;
        //     const ushort MessageIdFlag = 1 << 8;
        //     const ushort TimestampFlag = 1 << 7;
        //     const ushort TypeFlag = 1 << 6;
        //     const ushort UserIdFlag = 1 << 5;
        //     const ushort AppIdFlag = 1 << 4;
        //     const ushort ClusterIdFlag = 1 << 3;

        //     // Write properties in the order defined by the spec.
        //     if (props.TryGetValue("content-type", out var contentType))
        //     {
        //         flags |= ContentTypeFlag;
        //         writer.WriteNullableShortString(contentType.ToString());
        //     }
        //     if (props.TryGetValue("content-encoding", out var contentEncoding))
        //     {
        //         flags |= ContentEncodingFlag;
        //         writer.WriteNullableShortString(contentEncoding.ToString());
        //     }
        //     if (props.TryGetValue("delivery-mode", out var deliveryMode))
        //     {
        //         flags |= DeliveryModeFlag;
        //         writer.WriteByte(Convert.ToByte(deliveryMode));
        //     }
        //     if (props.TryGetValue("priority", out var priority))
        //     {
        //         flags |= PriorityFlag;
        //         writer.WriteByte(Convert.ToByte(priority));
        //     }
        //     if (props.TryGetValue("correlation-id", out var correlationId))
        //     {
        //         flags |= CorrelationIdFlag;
        //         writer.WriteNullableShortString(correlationId.ToString());
        //     }
        //     if (props.TryGetValue("reply-to", out var replyTo))
        //     {
        //         flags |= ReplyToFlag;
        //         writer.WriteNullableShortString(replyTo.ToString());
        //     }
        //     if (props.TryGetValue("expiration", out var expiration))
        //     {
        //         flags |= ExpirationFlag;
        //         writer.WriteNullableShortString(expiration.ToString());
        //     }
        //     if (props.TryGetValue("message-id", out var messageId))
        //     {
        //         flags |= MessageIdFlag;
        //         writer.WriteNullableShortString(messageId.ToString());
        //     }
        //     if (props.TryGetValue("timestamp", out var timestamp))
        //     {
        //         flags |= TimestampFlag;
        //         writer.WriteDateTime(Convert.ToDateTime(timestamp));
        //     }
        //     if (props.TryGetValue("type", out var type))
        //     {
        //         flags |= TypeFlag;
        //         writer.WriteNullableShortString(type.ToString());
        //     }
        //     if (props.TryGetValue("user-id", out var userId))
        //     {
        //         flags |= UserIdFlag;
        //         writer.WriteNullableShortString(userId.ToString());
        //     }
        //     if (props.TryGetValue("app-id", out var appId))
        //     {
        //         flags |= AppIdFlag;
        //         writer.WriteNullableShortString(appId.ToString());
        //     }
        //     if (props.TryGetValue("cluster-id", out var clusterId))
        //     {
        //         flags |= ClusterIdFlag;
        //         writer.WriteNullableShortString(clusterId.ToString());
        //     }

        //     // Write the flag field before the actual property values.
        //     // The writer above already emitted the values; we need to prepend flags.
        //     // Simpler approach: write flags first, then re‑write the stream.
        //     var propertyData = memoryStream.ToArray();
        //     using var finalStream = new MemoryStream();
        //     var finalWriter = new Amqp0_9_1Writer(finalStream);
        //     finalWriter.WriteUShort(flags);
        //     finalWriter.Write(propertyData);
        //     return finalStream.ToArray();
        // }
    }
}