namespace Amqp0_9_1.Encoding
{
    internal static class Amqp0_9_1Reader
    {
        public static bool DecodeBool(ReadOnlySpan<byte> data, ref int offset)
        {
            bool value = (data[offset] & 0x01) != 0;
            offset++;
            return value;
        }

        public static byte DecodeOctet(ReadOnlySpan<byte> data, ref int offset)
        {
            byte value = data[offset];
            offset++;
            return value;
        }

        public static ushort DecodeShort(ReadOnlySpan<byte> data, ref int offset)
        {
            ushort v = (ushort)((data[offset] << 8) | data[offset + 1]);
            offset += 2;
            return v;
        }

        public static uint DecodeLong(ReadOnlySpan<byte> data, ref int offset)
        {
            uint v = (uint)(
                (data[offset] << 24) |
                (data[offset + 1] << 16) |
                (data[offset + 2] << 8) |
                data[offset + 3]);
            offset += 4;
            return v;
        }

        public static ulong DecodeLongLong(ReadOnlySpan<byte> data, ref int offset)
        {
            ulong v = ((ulong)data[offset] << 56) |
                      ((ulong)data[offset + 1] << 48) |
                      ((ulong)data[offset + 2] << 40) |
                      ((ulong)data[offset + 3] << 32) |
                      ((ulong)data[offset + 4] << 24) |
                      ((ulong)data[offset + 5] << 16) |
                      ((ulong)data[offset + 6] << 8) |
                      data[offset + 7];
            offset += 8;
            return v;
        }

        public static string DecodeShortStr(ReadOnlySpan<byte> data, ref int offset)
        {
            int len = data[offset];
            offset++;
            string s = System.Text.Encoding.UTF8.GetString(data.Slice(offset, len));
            offset += len;
            return s;
        }

        public static string DecodeLongStr(ReadOnlySpan<byte> data, ref int offset)
        {
            uint len = DecodeLong(data, ref offset);
            string s = System.Text.Encoding.UTF8.GetString(data.Slice(offset, (int)len));
            offset += (int)len;
            return s;
        }

        public static DateTime DecodeTimestamp(ReadOnlySpan<byte> data, ref int offset)
        {
            ulong seconds = DecodeLongLong(data, ref offset);
            return DateTimeOffset.FromUnixTimeSeconds((long)seconds).UtcDateTime;
        }

        public static decimal DecodeDecimal(ReadOnlySpan<byte> data, ref int offset)
        {
            byte scale = data[offset];
            int intVal = (int)(
                (data[offset + 1] << 24) |
                (data[offset + 2] << 16) |
                (data[offset + 3] << 8) |
                data[offset + 4]);
            offset += 5;
            return new decimal(intVal, 0, 0, false, scale);
        }

        public static List<object> DecodeFieldArray(ReadOnlySpan<byte> data, ref int offset)
        {
            uint arrayLen = DecodeLong(data, ref offset);
            int end = offset + (int)arrayLen;
            var list = new List<object>();

            while (offset < end)
            {
                char type = (char)data[offset];
                offset++;

                switch (type)
                {
                    case 't': // boolean
                        list.Add(DecodeOctet(data, ref offset) != 0);
                        break;
                    case 'b': // signed 8‑bit
                        list.Add((sbyte)data[offset]);
                        offset++;
                        break;
                    case 'B': // unsigned 8‑bit
                        list.Add(DecodeOctet(data, ref offset));
                        break;
                    case 'U': // short (signed)
                        list.Add((short)DecodeShort(data, ref offset));
                        break;
                    case 'u': // unsigned short
                        list.Add(DecodeShort(data, ref offset));
                        break;
                    case 'I': // long (signed 32‑bit)
                        list.Add((int)DecodeLong(data, ref offset));
                        break;
                    case 'i': // unsigned long
                        list.Add(DecodeLong(data, ref offset));
                        break;
                    case 'L': // longlong (signed 64‑bit)
                        list.Add((long)DecodeLongLong(data, ref offset));
                        break;
                    case 'l': // unsigned longlong
                        list.Add(DecodeLongLong(data, ref offset));
                        break;
                    case 'D': // decimal
                        list.Add(DecodeDecimal(data, ref offset));
                        break;
                    case 's': // shortstr
                        list.Add(DecodeShortStr(data, ref offset));
                        break;
                    case 'S': // longstr
                        list.Add(DecodeLongStr(data, ref offset));
                        break;
                    case 'T': // timestamp
                        list.Add(DecodeTimestamp(data, ref offset));
                        break;
                    case 'F': // nested field‑table
                        list.Add(DecodeFieldTable(data, ref offset));
                        break;
                    case 'A': // nested array (recursive)
                        list.Add(DecodeFieldArray(data, ref offset));
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported field‑array element type '{type}'.");
                }
            }

            return list;
        }

        public static Dictionary<string, object> DecodeFieldTable(ReadOnlySpan<byte> data, ref int offset)
        {
            uint tableLen = DecodeLong(data, ref offset);
            int end = offset + (int)tableLen;
            var dict = new Dictionary<string, object>();

            while (offset < end)
            {
                string key = DecodeShortStr(data, ref offset);

                char type = (char)data[offset];
                offset++;

                object value = type switch
                {
                    't' => DecodeOctet(data, ref offset) != 0,
                    'b' => (sbyte)data[offset++],
                    'B' => DecodeOctet(data, ref offset),
                    'U' => (short)DecodeShort(data, ref offset),
                    'u' => DecodeShort(data, ref offset),
                    'I' => (int)DecodeLong(data, ref offset),
                    'i' => DecodeLong(data, ref offset),
                    'L' => (long)DecodeLongLong(data, ref offset),
                    'l' => DecodeLongLong(data, ref offset),
                    'D' => DecodeDecimal(data, ref offset),
                    's' => DecodeShortStr(data, ref offset),
                    'S' => DecodeLongStr(data, ref offset),
                    'T' => DecodeTimestamp(data, ref offset),
                    'F' => DecodeFieldTable(data, ref offset),
                    'A' => DecodeFieldArray(data, ref offset),
                    _   => throw new NotSupportedException($"Unsupported field‑table value type '{type}'.")
                };

                dict[key] = value;
            }

            return dict;
        }
    }
}
