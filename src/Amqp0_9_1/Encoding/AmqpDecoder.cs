namespace Amqp0_9_1.Encoding
{
    internal static class AmqpDecoder
    {
        public static bool Bool(ref ReadOnlyMemory<byte> buffer)
        {
            var value = (buffer.Span[0] & 0x01) != 0;

            buffer = buffer.Slice(1);
            return value;
        }

        public static byte Octet(ref ReadOnlyMemory<byte> buffer)
        {
            var value = buffer.Span[0];

            buffer = buffer.Slice(1);
            return value;
        }

        public static ushort Short(ref ReadOnlyMemory<byte> buffer)
        {
            var value = (ushort)(buffer.Span[0] << 8 | buffer.Span[1]);
            buffer = buffer.Slice(2);
            return value;
        }

        public static uint Long(ref ReadOnlyMemory<byte> buffer)
        {
            var value = (uint)(
                buffer.Span[0] << 24 |
                buffer.Span[1] << 16 |
                buffer.Span[2] << 8 |
                buffer.Span[3]);

            buffer = buffer.Slice(4);
            return value;
        }

        public static ulong LongLong(ref ReadOnlyMemory<byte> buffer)
        {
            var value = (ulong)buffer.Span[0] << 56 |
                        (ulong)buffer.Span[1] << 48 |
                        (ulong)buffer.Span[ 2] << 40 |
                        (ulong)buffer.Span[3] << 32 |
                        (ulong)buffer.Span[4] << 24 |
                        (ulong)buffer.Span[5] << 16 |
                        (ulong)buffer.Span[6] << 8 |
                        buffer.Span[7];

            buffer = buffer.Slice(8);
            return value;
        }

        public static char Char(ref ReadOnlyMemory<byte> buffer)
        {
            var charValue = (char)buffer.Span[0];
            buffer = buffer.Slice(1);
            return charValue;
        }

        public static string ShortString(ref ReadOnlyMemory<byte> buffer)
        {
            int len = buffer.Span[0];
            var stringValue = System.Text.Encoding.UTF8.GetString(buffer.ToArray(), 1, len);

            buffer = buffer.Slice(1 + len);
            return stringValue;
        }

        public static string LongString(ref ReadOnlyMemory<byte> buffer)
        {
            var len = (int)Long(ref buffer);
            var stringValue = System.Text.Encoding.UTF8.GetString(buffer.ToArray(), 0, len);

            buffer = buffer.Slice(len);
            return stringValue;
        }

        public static DateTime Timestamp(ref ReadOnlyMemory<byte> buffer)
        {
            var seconds = LongLong(ref buffer);
            return DateTimeOffset.FromUnixTimeSeconds((long)seconds).UtcDateTime;
        }

        public static decimal Decimal(ref ReadOnlyMemory<byte> buffer)
        {
            var scale = buffer.Span[0];
            var intVal = 
                buffer.Span[1] << 24 |
                buffer.Span[2] << 16 |
                buffer.Span[3] << 8 |
                buffer.Span[4];

            buffer = buffer.Slice(5);
            return new decimal(intVal, 0, 0, false, scale);
        }

        public static List<object> Array(ref ReadOnlyMemory<byte> buffer)
        {
            var arrayLen = Long(ref buffer);
            var end = (int)arrayLen;
            var list = new List<object>();

            while (buffer.Length < end)
            {
                var type = (char)buffer.Span[0];

                buffer = buffer.Slice(1);

                switch (type)
                {
                    case 't':
                        list.Add(Octet(ref buffer) != 0);
                        break;
                    case 'b':
                        list.Add((sbyte)Octet(ref buffer));
                        break;
                    case 'B':
                        list.Add(Octet(ref buffer));
                        break;
                    case 'U':
                        list.Add((short)Short(ref buffer));
                        break;
                    case 'u':
                        list.Add(Short(ref buffer));
                        break;
                    case 'I':
                        list.Add((int)Long(ref buffer));
                        break;
                    case 'i':
                        list.Add(Long(ref buffer));
                        break;
                    case 'L':
                        list.Add((long)LongLong(ref buffer));
                        break;
                    case 'l':
                        list.Add(LongLong(ref buffer));
                        break;
                    case 'D':
                        list.Add(Decimal(ref buffer));
                        break;
                    case 's':
                        list.Add(ShortString(ref buffer));
                        break;
                    case 'S':
                        list.Add(LongString(ref buffer));
                        break;
                    case 'T':
                        list.Add(Timestamp(ref buffer));
                        break;
                    case 'F':
                        list.Add(Table(ref buffer));
                        break;
                    case 'A':
                        list.Add(Array(ref buffer));
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported field-array element type '{type}'.");
                }
            }

            return list;
        }

        public static Dictionary<string, object> Table(ref ReadOnlyMemory<byte> buffer)
        {
            var tableLen = Long(ref buffer);

            if(tableLen == 1)
                return [];
        
            var dict = new Dictionary<string, object>();
            var tableBuffer = buffer.Slice(0, (int)tableLen);

            while (!tableBuffer.IsEmpty)
            {
                var key = ShortString(ref tableBuffer);

                var type = Char(ref tableBuffer);

                object value = type switch
                {
                    't' => Octet(ref tableBuffer) != 0,
                    'b' => (sbyte)Octet(ref tableBuffer),
                    'B' => Octet(ref tableBuffer),
                    'U' => (short)Short(ref tableBuffer),
                    'u' => Short(ref tableBuffer),
                    'I' => (int)Long(ref tableBuffer),
                    'i' => Long(ref tableBuffer),
                    'L' => (long)LongLong(ref tableBuffer),
                    'l' => LongLong(ref tableBuffer),
                    'D' => Decimal(ref tableBuffer),
                    's' => ShortString(ref tableBuffer),
                    'S' => LongString(ref tableBuffer),
                    'T' => Timestamp(ref tableBuffer),
                    'F' => Table(ref tableBuffer),
                    'A' => Array(ref tableBuffer),
                    _ => throw new NotSupportedException($"Unsupported field-table value type '{type}'.")
                };

                dict[key] = value;
            }

            buffer = buffer.Slice((int)tableLen);
            return dict;
        }
    }
}
