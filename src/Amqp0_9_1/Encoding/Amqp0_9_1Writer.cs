using System.Buffers;

namespace Amqp0_9_1.Encoding
{
    internal static class Amqp0_9_1Writer
    {
        public static ReadOnlySpan<byte> EncodeBool(bool value) =>
            new[] { (byte)(value ? 1 : 0) };

        public static ReadOnlySpan<byte> EncodeOctet(byte value) =>
            new[] { value };

        public static ReadOnlySpan<byte> EncodeShort(ushort value) =>
            new[]
            {
                (byte)(value >> 8),
                (byte)value
            };

        public static ReadOnlySpan<byte> EncodeLong(uint value) =>
            new[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };

        public static ReadOnlySpan<byte> EncodeLongLong(ulong value) =>
            new[]
            {
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };

        public static ReadOnlySpan<byte> EncodeShortStr(string value)
        {
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
            if (utf8.Length > 255)
                throw new ArgumentException("Short string exceeds 255 bytes.");

            var writer = new ArrayBufferWriter<byte>();
            writer.Write([(byte)utf8.Length]);
            writer.Write(utf8);
            return writer.WrittenSpan;
        }

        public static ReadOnlySpan<byte> EncodeLongStr(string value)
        {
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
            var writer = new ArrayBufferWriter<byte>();
            writer.Write(EncodeLong((uint)utf8.Length));
            writer.Write(utf8);
            return writer.WrittenSpan;
        }

        public static ReadOnlySpan<byte> EncodeTimestamp(DateTime utcTime)
        {
            long seconds = ((DateTimeOffset)utcTime).ToUnixTimeSeconds();
            return EncodeLongLong((ulong)seconds);
        }

        public static ReadOnlySpan<byte> EncodeDecimal(decimal value)
        {
            int[] bits = decimal.GetBits(value);
            byte scale = (byte)((bits[3] >> 16) & 0x7F);
            int intVal = bits[0];

            var writer = new ArrayBufferWriter<byte>();
            var tempSpan = writer.GetSpan(5);
            tempSpan[0] = scale;
            tempSpan[1] = (byte)(intVal >> 24);
            tempSpan[2] = (byte)(intVal >> 16);
            tempSpan[3] = (byte)(intVal >> 8);
            tempSpan[4] = (byte)intVal;
            writer.Advance(5);
            return writer.WrittenSpan;
        }

        public static ReadOnlySpan<byte> EncodeFieldArray(IList<object> values)
        {
            var writer = new ArrayBufferWriter<byte>();

            writer.Write(new byte[4]);

            foreach (var v in values)
            {
                WriteFieldValue(writer, v);
            }

            int length = writer.WrittenCount - 4;
            var buffer = writer.WrittenSpan.ToArray();
            EncodeLong((uint)length).CopyTo(buffer);
            return buffer;
        }

        public static ReadOnlySpan<byte> EncodeFieldTable(IDictionary<string, object> table)
        {
            var writer = new ArrayBufferWriter<byte>();

            writer.Write(new byte[4]);

            foreach (var kvp in table)
            {
                writer.Write(EncodeShortStr(kvp.Key));
                WriteFieldValue(writer, kvp.Value);
            }

            int length = writer.WrittenCount - 4;
            var buffer = writer.WrittenSpan.ToArray();
            EncodeLong((uint)length).CopyTo(buffer);
            return buffer;
        }

        private static void WriteFieldValue(IBufferWriter<byte> writer, object value)
        {
            switch (value)
            {
                case bool bool_value:
                    writer.Write([(byte)'t']);;
                    writer.Write([bool_value ? (byte)1 : (byte)0]);
                    break;

                case sbyte sbyte_value:
                    writer.Write([(byte)'b']);
                    writer.Write([(byte)sbyte_value]);
                    break;

                case byte byte_value:
                    writer.Write([(byte)'B']);
                    writer.Write([byte_value]);
                    break;

                // Rabbitmq 3.13 use 's' for type short int - 
                // https://github.com/jbrisbin/rabbit_common/blob/master/src/rabbit_binary_parser.erl#L64
                case short short_value:
                    writer.Write([(byte)'s']);
                    writer.Write(EncodeShort((ushort)short_value));
                    break;

                case ushort ushort_value:
                    writer.Write([(byte)'u']);
                    writer.Write(EncodeShort(ushort_value));
                    break;

                case int int_value:
                    writer.Write([(byte)'I']);
                    writer.Write(EncodeLong((uint)int_value));
                    break;

                case uint uint_value:
                    writer.Write([(byte)'i']);
                    writer.Write(EncodeLong(uint_value));
                    break;

                // I don't found 'L' in Rabbitmq 3.13 - 
                // https://github.com/jbrisbin/rabbit_common/blob/master/src/rabbit_binary_parser.erl#L61-L98
                // case long long_value:
                //     writer.Write([(byte)'L']);
                //     writer.Write(EncodeLongLong((ulong)long_value));
                //     break;

                case ulong ulong_value:
                    writer.Write([(byte)'l']);
                    writer.Write(EncodeLongLong(ulong_value));
                    break;

                case decimal decimal_value:
                    writer.Write([(byte)'D']);
                    writer.Write(EncodeDecimal(decimal_value));
                    break;

                // Error on short-string - https://www.rabbitmq.com/amqp-0-9-1-errata
                case string string_value:
                    writer.Write([(byte)'S']);
                    writer.Write(EncodeLongStr(string_value));
                    break;

                case DateTime dateTime_value:
                    writer.Write([(byte)'T']);
                    writer.Write(EncodeTimestamp(dateTime_value));
                    break;

                case IDictionary<string, object> nestedTable:
                    writer.Write([(byte)'F']);
                    writer.Write(EncodeFieldTable(nestedTable));
                    break;

                case IList<object> nestedArray:
                    writer.Write([(byte)'A']);
                    writer.Write(EncodeFieldArray(nestedArray));
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported field value type '{value?.GetType()}'.");
            }
        }
    }
}
