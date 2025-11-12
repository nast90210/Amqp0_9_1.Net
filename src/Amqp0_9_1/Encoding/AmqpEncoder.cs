using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Encoding
{
    internal static class AmqpEncoder
    {
        public static byte Bool(bool value) => (byte)(value ? 1 : 0);

        public static ReadOnlyMemory<byte> Short(ushort value) =>
            new([
                (byte)(value >> 8),
                (byte)value
            ]);

        public static ReadOnlyMemory<byte> Long(uint value) =>
            new([
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            ]);

        public static ReadOnlyMemory<byte> LongLong(ulong value) =>
            new([
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            ]);

        public static ReadOnlyMemory<byte> ShortString(string value)
        {
            var stringBuffer = System.Text.Encoding.UTF8.GetBytes(value);
            if (stringBuffer.Length > 255)
                throw new ArgumentException("Short string exceeds 255 bytes.");

            using var buffer = new MemoryBuffer();
            buffer.Write((byte)stringBuffer.Length);
            buffer.Write(stringBuffer);
            return buffer.WrittenMemory;
        }

        public static ReadOnlyMemory<byte> LongString(string value)
        {
            var stringBuffer = System.Text.Encoding.UTF8.GetBytes(value);

            using var buffer = new MemoryBuffer();
            buffer.Write(Long((uint)stringBuffer.Length));
            buffer.Write(stringBuffer);
            return buffer.WrittenMemory;
        }

        public static ReadOnlyMemory<byte> Timestamp(DateTime utcTime)
        {
            var seconds = ((DateTimeOffset)utcTime).ToUnixTimeSeconds();
            return LongLong((ulong)seconds);
        }

        public static ReadOnlyMemory<byte> Decimal(decimal value)
        {
            var bits = decimal.GetBits(value);
            var scale = (byte)(bits[3] >> 16 & 0x7F);
            var intVal = bits[0];

            using var buffer = new MemoryBuffer(5);
            buffer.Write(scale);
            buffer.Write((byte)(intVal >> 24));
            buffer.Write((byte)(intVal >> 16));
            buffer.Write((byte)(intVal >> 8));
            buffer.Write((byte)intVal);
            return buffer.WrittenMemory;
        }

        public static ReadOnlyMemory<byte> Array(IList<object> values)
        {
            using var arrayBuffer = new MemoryBuffer();
            foreach (var value in values)
            {
                EncodeFieldValue(arrayBuffer, value);
            }

            using var buffer = new MemoryBuffer();
            buffer.Write(Long((uint)arrayBuffer.Length));
            buffer.Write(arrayBuffer.WrittenMemory);
            return buffer.WrittenMemory;
        }

        public static ReadOnlyMemory<byte> Table(IDictionary<string, object> table)
        {
            using var tableBuffer = new MemoryBuffer();

            foreach (var pair in table)
            {
                tableBuffer.Write(ShortString(pair.Key));
                EncodeFieldValue(tableBuffer, pair.Value);
            }

            using var buffer = new MemoryBuffer();
            buffer.Write(Long((uint)tableBuffer.Length));
            buffer.Write(tableBuffer.WrittenMemory);
            return buffer.WrittenMemory;
        }

        private static void EncodeFieldValue(MemoryBuffer buffer, object value)
        {
            switch (value)
            {
                case bool boolValue:
                    buffer.Write((byte)'t');
                    buffer.Write(boolValue ? (byte)1 : (byte)0);
                    break;

                case sbyte sbyteValue:
                    buffer.Write((byte)'b');
                    buffer.Write((byte)sbyteValue);
                    break;

                case byte byteValue:
                    buffer.Write((byte)'B');
                    buffer.Write(byteValue);
                    break;

                // Rabbitmq 3.13 use 's' for type short int - 
                // https://github.com/jbrisbin/rabbit_common/blob/master/src/rabbit_binary_parser.erl#L64
                case short shortValue:
                    buffer.Write((byte)'s');
                    buffer.Write(Short((ushort)shortValue));
                    break;

                case ushort ushortValue:
                    buffer.Write((byte)'u');
                    buffer.Write(Short(ushortValue));
                    break;

                case int intValue:
                    buffer.Write((byte)'I');
                    buffer.Write(Long((uint)intValue));
                    break;

                case uint uintValue:
                    buffer.Write((byte)'i');
                    buffer.Write(Long(uintValue));
                    break;

                // I don't found 'L' in Rabbitmq 3.13 - 
                // https://github.com/jbrisbin/rabbit_common/blob/master/src/rabbit_binary_parser.erl#L61-L98
                // case long long_value:
                //     Write(ref buffer, (byte)'L', LongLong((ulong)long_value));
                //     break;

                case ulong ulongValue:
                    buffer.Write((byte)'l');
                    buffer.Write(LongLong(ulongValue));
                    break;

                case decimal decimalValue:
                    buffer.Write((byte)'D');
                    buffer.Write(Decimal(decimalValue));
                    break;

                // Error on short-string - https://www.rabbitmq.com/amqp-0-9-1-errata
                case string stringValue:
                    buffer.Write((byte)'S');
                    buffer.Write(LongString(stringValue));
                    break;

                case DateTime dateTimeValue:
                    buffer.Write((byte)'T');
                    buffer.Write(Timestamp(dateTimeValue));
                    break;

                case IDictionary<string, object> nestedTable:
                    buffer.Write((byte)'F');
                    buffer.Write(Table(nestedTable));
                    break;

                case IList<object> nestedArray:
                    buffer.Write((byte)'A');
                    buffer.Write(Array(nestedArray));
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported field value type '{value.GetType()}'.");
            }
        }

    }
}
