# Amqp0_9_1.Net

A lightweight .NET implementation of the AMQP 0‑9‑1 protocol, offering an easy‑to‑use, asynchronous API for communicating with RabbitMQ and other AMQP‑compatible brokers.

---

## Features

- Current support for AMQP 0‑9‑1 frames:
  - Methods: BasicAck, BasicConsume, BasicConsumeOk, BasicDeliver, BasicNack, ChannelClose, ChannelCloseOk, ChannelOpen, ChannelOpenOk, ConnectionClose, ConnectionOpen, ConnectionOpenOk, ConnectionStart, ConnectionStartOk, ConnectionTune, ConnectionTuneOk, ExchangeDeclare, QueueBind, QueueDeclare.
  - Headers: full support on consume 
  - Body: all popular body encoding
  - Heartbeat: full support
- Asynchronous `async/await` API  
- Currently, no automatic reconnection  
- Currently, only TCP (no TLS) 

---

## Dependencies

- .NET Standard 2.0
- System.IO.Pipelines
- System.Threading.Channels

---

## Getting Started

### Installation

```bash
dotnet add package Amqp0_9_1.Net
```

### Basic Example

```csharp
using Amqp0_9_1.Clients;
using Amqp0_9_1.Constants;

// Create a connection factory
using var connection = new AmqpConnection("localhost", 5672);

// Connect to AMQP server
await connection.ConnectAsync("guest", "guest");

//Open new channel
using var channel = await connection.CreateChannelAsync(1);

// Declare (create or update) Exchange
var exchange = await channel.ExchangeDeclareAsync("exchange_name", ExchangeType.Direct);

// Declare (create or update) Queue
var queue = await channel.QueueDeclareAsync("queue_name");

// Bind Exchange to Queue
await queue.BindAsync("exchange_name", "queue_name");

// Consume messages from Queue
await queue.ConsumeAsync(async message =>
{
    Console.WriteLine("Message body: " + message.Body);
    await queue.AckAsync(message.DeliveryTag);
});

```

---

## Configuration

| Setting               | Description                              | Default |
|-----------------------|------------------------------------------|---------|
| `host`                | Broker hostname or IP                    | `localhost` |
| `port`                | Broker port (currently, only plain)      | `5672`  |
| `username`            | Authentication user                      | `guest` |
| `password`            | Authentication password                  | `guest` |
| `virtualHost`         | AMQP virtual host                        | `/` |

---

## Contributing

1. Fork the repository.  
2. Create a feature branch (`git checkout -b feature/xyz`).  
3. Write tests for your changes.  
4. Ensure all tests pass (`dotnet test`).  
5. Submit a pull request.

Please follow the existing coding style and include XML documentation for public members.

---

## License

This project is licensed under the [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE). See the `LICENSE` file for full details.

---

## References & Resources

- AMQP 0‑9‑1 Specification – https://www.rabbitmq.com/resources/specs/amqp0-9-1.pdf