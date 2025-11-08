using Amqp0_9_1.Primitives.Frames;
using Amqp0_9_1.Encoding;
using Amqp0_9_1.Methods.Basic;
using Amqp0_9_1.Methods.Channel;
using Amqp0_9_1.Methods.Connection;
using Amqp0_9_1.Methods.Constants;

namespace Amqp0_9_1.Methods
{
    internal static class MethodFactory
    {
        internal static AmqpMethod Create(AmqpRawFrame methodRawFrame)
        {
            var payload = methodRawFrame.Payload;
            var classId = AmqpDecoder.Short(ref payload);
            var methodId = AmqpDecoder.Short(ref payload);

            switch (classId)
            {
                case MethodClassId.Connection:
                    switch (methodId)
                    {
                        case ConnectionMethodId.Start:
                            return new ConnectionStart(payload);
                        case ConnectionMethodId.Tune:
                            return new ConnectionTune(payload);
                        case ConnectionMethodId.OpenOk:
                            return new ConnectionOpenOk(payload);
                        case ConnectionMethodId.ConnectionClose:
                            return new ConnectionClose(payload);
                    }
                    break;
                case MethodClassId.Channel:
                    switch (methodId)
                    {
                        case ChannelMethodId.OpenOk:
                            return new ChannelOpenOk(payload);
                        case ChannelMethodId.CloseOk:
                            return new ChannelCloseOk();
                    }
                    break;
                case MethodClassId.Basic:
                    switch(methodId)
                    {
                        case BasicMethodId.ConsumeOk:
                            return new BasicConsumeOk(payload);
                        case BasicMethodId.Deliver:
                            return new BasicDeliver(payload);
                    }
                    break;
                default:
                    throw new NotSupportedException($"Unknown class-id {classId}.");
            }

            throw new NotSupportedException($"Unknown method-id {methodId} for class-id {classId}.");
        }
    }

}
