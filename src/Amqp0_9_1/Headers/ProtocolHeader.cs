namespace Amqp0_9_1.Headers
{
    public sealed class ProtocolHeader
    {
        private const string ProtocolVersion = "AMQP\x00\x00\x09\x01";

        public static byte[] GetPayload()
        {
            return System.Text.Encoding.ASCII.GetBytes(ProtocolVersion);
        }
    }
}