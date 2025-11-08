using Amqp0_9_1.Encoding;
using Amqp0_9_1.Utilities;

namespace Amqp0_9_1.Primitives.SASL
{
    internal sealed class SaslPlainResponse
    {
        private readonly string _username;
        private readonly string _password;

        public SaslPlainResponse(string username, string password)
        {
            _username = username;
            _password = password;
        }

        private ArraySegment<byte> UsernameByteArray => new(System.Text.Encoding.UTF8.GetBytes(_username));
        private ArraySegment<byte> PasswordByteArray => new(System.Text.Encoding.UTF8.GetBytes(_password));
        private ReadOnlyMemory<byte> Length => AmqpEncoder.Long((uint)(1 + UsernameByteArray.Count + 1 + PasswordByteArray.Count));

        public ReadOnlyMemory<byte> GetPayload()
        {
            using var buffer = new MemoryBuffer();
            buffer.Write(Length);
            buffer.Write(0x00);
            buffer.Write(UsernameByteArray);
            buffer.Write(0x00);
            buffer.Write(PasswordByteArray);
            return buffer.WrittenMemory;
        }
    }
}