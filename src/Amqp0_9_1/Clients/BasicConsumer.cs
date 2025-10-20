// using System.Collections.Concurrent;

// namespace Amqp0_9_1.Clients
// {
//     /// <summary>
//     /// Потребитель сообщений, совместимый с .NET Standard 2.0.
//     /// Вместо <c>System.Threading.Channels</c> используется <c>BlockingCollection</c>,
//     /// который доступен в этой версии фреймворка.
//     /// </summary>
//     public sealed class BasicConsumer : IDisposable
//     {
//         private readonly AmqpChannel _channel;
//         private readonly string _consumerTag;

//         // Очередь для поступающих сообщений
//         private readonly BlockingCollection<BasicDeliverEventArgs> _msgQueue
//             = new BlockingCollection<BasicDeliverEventArgs>(new ConcurrentQueue<BasicDeliverEventArgs>());

//         // Хранилище ожидающих подтверждений сообщений
//         private readonly ConcurrentDictionary<ulong, BasicDeliverEventArgs> _pendingDeliveries
//             = new ConcurrentDictionary<ulong, BasicDeliverEventArgs>();

//         private bool _disposed;

//         public BasicConsumer(AmqpChannel channel, string consumerTag)
//         {
//             _channel = channel ?? throw new ArgumentNullException(nameof(channel));
//             _consumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));

//             // В полной реализации здесь регистрируется диспетчер,
//             // который направляет фреймы Basic.Deliver / Basic.ContentHeader / Basic.Body
//             // к этому потребителю по consumerTag.
//         }

//         public string ConsumerTag => _consumerTag;

//         /// <summary>
//         /// Асинхронно ждёт следующего сообщения.
//         /// </summary>
//         public Task<BasicDeliverEventArgs?> ReadAsync(CancellationToken ct = default)
//         {
//             // Если токен уже отменён – сразу возвращаем отменённую задачу.
//             if (ct.IsCancellationRequested)
//                 return Task.FromCanceled<BasicDeliverEventArgs?>(ct);

//             return Task.Run(() =>
//             {
//                 // Блокируемся до появления элемента или отмены токена.
//                 while (!ct.IsCancellationRequested)
//                 {
//                     BasicDeliverEventArgs? item;
//                     if (_msgQueue.TryTake(out item, Timeout.Infinite, ct))
//                         return item;
//                 }

//                 // Если вышли из цикла – токен отменён.
//                 ct.ThrowIfCancellationRequested();
//                 // Эта строка недостижима, но компилятору нужен return.
//                 return null;
//             }, ct);
//         }

//         // Внутренний метод, вызываемый диспетчером фреймов
//         internal void EnqueueMessage(BasicDeliverEventArgs args)
//         {
//             if (args == null) throw new ArgumentNullException(nameof(args));

//             // Сохраняем сообщение, чтобы позже можно было подтвердить/отклонить его.
//             _pendingDeliveries[args.DeliveryTag] = args;

//             // Добавляем в очередь; если очередь уже закрыта, сообщение будет отброшено.
//             _msgQueue.Add(args);
//         }

//         /// <summary>
//         /// Подтверждает обработку сообщения.
//         /// </summary>
//         public void Ack(ulong deliveryTag, bool multiple = false)
//         {
//             if (_pendingDeliveries.TryRemove(deliveryTag, out _))
//                 _channel.BasicAckAsync(deliveryTag, multiple);
//         }

//         /// <summary>
//         /// Отклоняет сообщение (можно переотправить в очередь).
//         /// </summary>
//         public void Nack(ulong deliveryTag, bool multiple = false, bool requeue = true)
//         {
//             if (_pendingDeliveries.TryRemove(deliveryTag, out _))
//                 _channel.BasicNackAsync(deliveryTag, multiple, requeue);
//         }

//         /// <summary>
//         /// Корректно завершает работу потребителя и освобождает ресурсы.
//         /// </summary>
//         public void Dispose()
//         {
//             if (_disposed) return;
//             _disposed = true;

//             // Останавливаем приём новых сообщений.
//             _msgQueue.CompleteAdding();

//             // Очищаем оставшиеся сообщения, если они есть.
//             while (_msgQueue.TryTake(out _)) { }

//             // При необходимости можно отменить подписку на сервере.
//             // _channel.BasicCancel(_consumerTag);

//             _pendingDeliveries.Clear();
//         }
//     }
// }
