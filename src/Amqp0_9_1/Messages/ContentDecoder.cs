using System.IO.Compression;

namespace Amqp0_9_1.Messages
{
    internal static class ContentDecoder
    {
        public static string Decode(string? encoding, ReadOnlyMemory<byte> buffer)
        {
            if (string.IsNullOrEmpty(encoding))
                return System.Text.Encoding.UTF8.GetString(buffer.ToArray());

            return encoding switch
            {
                "identity" => System.Text.Encoding.UTF8.GetString(buffer.ToArray()),
                "utf-8" => System.Text.Encoding.UTF8.GetString(buffer.ToArray()),
                "utf8" => System.Text.Encoding.UTF8.GetString(buffer.ToArray()),
                "utf-16" => System.Text.Encoding.Unicode.GetString(buffer.ToArray()),
                "utf-16be" => System.Text.Encoding.BigEndianUnicode.GetString(buffer.ToArray()),
                "utf-32" => System.Text.Encoding.UTF32.GetString(buffer.ToArray()),
                "ascii" => System.Text.Encoding.ASCII.GetString(buffer.ToArray()),
                "gzip" => DecompressGzip(buffer),
                "deflate" => DecompressDeflate(buffer),
                "base64" => DecodeBase64(buffer),
                _ => throw new NotSupportedException($"Encoding '{encoding}' is not supported.")
            };
        }

        private static string DecompressGzip(ReadOnlyMemory<byte> buffer)
        {
            using var ms = new MemoryStream(buffer.ToArray());
            using var gz = new GZipStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(gz, System.Text.Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static string DecompressDeflate(ReadOnlyMemory<byte> buffer)
        {
            using var ms = new MemoryStream(buffer.ToArray());
            using var df = new DeflateStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(df, System.Text.Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static string DecodeBase64(ReadOnlyMemory<byte> buffer)
        {
            var base64String = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
            var decodedBytes = Convert.FromBase64String(base64String);
            return System.Text.Encoding.UTF8.GetString(decodedBytes);
        }
    }
}