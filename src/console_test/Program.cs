using Amqp0_9_1.Clients;
using Amqp0_9_1.Constants;

bool IsConsumed = false;
try
{
    using var connection = new AmqpConnection("localhost", 5672);
    await connection.ConnectAsync("guest", "guest");
    using (var channel = await connection.CreateChannelAsync(1))
    {
        var exchange = await channel.ExchangeDeclareAsync("test", ExchangeType.Direct);
        var queue = await channel.QueueDeclareAsync("test");
        await queue.BindAsync("test", "test");
        await queue.ConsumeAsync(async message =>
        {
            Console.WriteLine("Message body: " + message.Body);
            await queue.NackAsync(message.DeliveryTag);
        });
    }

    while (!IsConsumed)
    {
        await Task.Delay(1000);
    }
}
catch (System.Exception)
{
    throw;
}
