using System.Text;
using Amqp.Core.Services;

namespace Amqp.Core.Primitives.SASL
{
    internal sealed class SaslPlainResponse(string username, string password)
    {
        private readonly string _username = username;
        private readonly string _password = password;

        private byte[] UsernameByteArray => Encoding.UTF8.GetBytes(_username);
        private byte[] PasswordByteArray => Encoding.UTF8.GetBytes(_password);

        public int Length => 1 + UsernameByteArray.Length + 1 + PasswordByteArray.Length;

        internal byte[] AsArray()
        {
            using var buffer = new ArrayBuffer();
            buffer.Write(0x00);
            buffer.Write(UsernameByteArray);
            buffer.Write(0x00);
            buffer.Write(PasswordByteArray);
            return buffer.ToArray();
        }
    }
}